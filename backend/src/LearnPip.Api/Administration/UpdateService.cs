using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

public sealed record ReleaseInfo(string Version, string Name, string Notes, string Url, DateTimeOffset? PublishedAtUtc);
public sealed record UpdateStatus(
    string InstalledVersion, string? LatestVersion, string State, string Interval,
    DateTimeOffset? LastCheckedAtUtc, DateTimeOffset? NextCheckAtUtc, string? Error,
    ReleaseInfo? Release, UpdateJob? Job);

public sealed class UpdateJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ActorAccountId { get; init; }
    public string FromVersion { get; init; } = "";
    public string TargetVersion { get; init; } = "";
    public string State { get; set; } = "queued";
    public string Phase { get; set; } = "queued";
    public string? Message { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UpdateService(
    LearnPipDbContext db, IHttpClientFactory clients, IConfiguration configuration, ILogger<UpdateService> logger)
{
    private const string Repo = "kennfarbe/LearnPip";
    private const string IntervalKey = "update_check_interval";
    private const string LastCheckKey = "update_last_check_utc";
    private const string LatestKey = "update_latest_version";
    private const string ReleaseKey = "update_latest_release";
    private const string JobKey = "update_job";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex Stable = new(@"^v\d+\.\d+\.\d+$", RegexOptions.Compiled);
    private static readonly string[] Intervals = ["daily", "weekly", "monthly", "never"];

    public string InstalledVersion
    {
        get
        {
            var configured = configuration["LearnPip:Version"];
            if (configured is not null && Stable.IsMatch(configured)) return configured;

            var environment = Environment.GetEnvironmentVariable("LEARNPIP_VERSION");
            return environment is not null && Stable.IsMatch(environment) ? environment : "unknown";
        }
    }

    public async Task CheckScheduledAsync(CancellationToken ct)
    {
        var status = await StatusAsync(ct);
        if (status.Interval == "never") return;
        if (status.LastCheckedAtUtc is null || status.NextCheckAtUtc is null ||
            DateTimeOffset.UtcNow >= status.NextCheckAtUtc)
            await CheckAsync(true, ct);
    }

    public async Task<UpdateStatus> StatusAsync(CancellationToken ct)
    {
        await ImportOperatorStatusAsync(ct);
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(x => new[] { IntervalKey, LastCheckKey, LatestKey, ReleaseKey, JobKey }.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        var interval = settings.GetValueOrDefault(IntervalKey, "daily");
        var last = ParseDate(settings.GetValueOrDefault(LastCheckKey));
        var release = Deserialize<ReleaseInfo>(settings.GetValueOrDefault(ReleaseKey));
        var latest = settings.GetValueOrDefault(LatestKey);
        var job = Deserialize<UpdateJob>(settings.GetValueOrDefault(JobKey));
        var state = "unknown";
        if (latest is not null)
        {
            state = Compare(latest, InstalledVersion) > 0 ? "update_available" : "current";
        }
        if (job is { State: "queued" or "running" }) state = "updating";
        return new(InstalledVersion, latest, state, interval, last, Next(last, interval), null, release, job);
    }

    public async Task<UpdateStatus> CheckAsync(bool force, CancellationToken ct)
    {
        var current = await StatusAsync(ct);
        if (!force && current.LastCheckedAtUtc is not null && current.NextCheckAtUtc is { } next &&
            DateTimeOffset.UtcNow < next) return current;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{Repo}/releases?per_page=20");
            request.Headers.UserAgent.ParseAdd("LearnPip-update-checker/1");
            using var response = await clients.CreateClient("github-releases").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"GitHub release metadata returned HTTP {(int)response.StatusCode}.");
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var release = json.RootElement.EnumerateArray()
                .Where(x => !x.GetProperty("draft").GetBoolean() && !x.GetProperty("prerelease").GetBoolean())
                .Select(x => new ReleaseInfo(
                    x.GetProperty("tag_name").GetString() ?? "",
                    x.GetProperty("name").GetString() ?? x.GetProperty("tag_name").GetString() ?? "",
                    x.GetProperty("body").GetString() ?? "",
                    x.GetProperty("html_url").GetString() ?? "",
                    x.TryGetProperty("published_at", out var p) && p.ValueKind == JsonValueKind.String
                        ? p.GetDateTimeOffset() : null))
                .Where(x => Stable.IsMatch(x.Version) &&
                    x.Url.StartsWith($"https://github.com/{Repo}/releases/", StringComparison.Ordinal))
                .OrderByDescending(x => SemVersion(x.Version)).FirstOrDefault()
                ?? throw new InvalidOperationException("No stable LearnPip release was returned.");
            var now = DateTimeOffset.UtcNow;
            await PutAsync(LastCheckKey, now.ToString("O"), ct);
            await PutAsync(LatestKey, release.Version, ct);
            await PutAsync(ReleaseKey, JsonSerializer.Serialize(release, JsonOptions), ct);
            return await StatusAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Stable release check failed.");
            var failed = await StatusAsync(ct);
            return failed with { State = "check_failed", Error = "Release-Prüfung fehlgeschlagen. Bitte später erneut versuchen." };
        }
    }

    public async Task<bool> SetIntervalAsync(string interval, CancellationToken ct)
    {
        if (!Intervals.Contains(interval)) return false;
        await PutAsync(IntervalKey, interval, ct);
        return true;
    }

    public async Task<(UpdateJob? Job, string? Error)> QueueAsync(Guid actor, string target, CancellationToken ct)
    {
        if (!Stable.IsMatch(target)) return (null, "invalid_version");
        var status = await CheckAsync(true, ct);
        if (status.Release?.Version != target || Compare(target, InstalledVersion) <= 0)
            return (null, "unverified_release");
        var queue = configuration["LearnPip:UpdateQueuePath"];
        var operatorStatus = configuration["LearnPip:UpdateStatusPath"];
        if (string.IsNullOrWhiteSpace(queue) || !Directory.Exists(queue) ||
            string.IsNullOrWhiteSpace(operatorStatus) || !Directory.Exists(operatorStatus))
            return (null, "operator_unavailable");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1049071811)", ct);
        var existing = Deserialize<UpdateJob>((await db.SystemSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == JobKey, ct))?.Value);
        if (existing is { State: "queued" or "running" }) return (null, "update_in_progress");
        var job = new UpdateJob { ActorAccountId = actor, FromVersion = InstalledVersion, TargetVersion = target };
        await PutAsync(JobKey, JsonSerializer.Serialize(job, JsonOptions), ct);
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actor,
            Action = "update.queued",
            Target = $"release:{target}",
            PreviousValue = InstalledVersion,
            NewValue = target
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        try
        {
            var tmp = Path.Combine(queue, $".{job.Id:N}.tmp");
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(job, JsonOptions), ct);
            File.Move(tmp, Path.Combine(queue, $"{job.Id:N}.json"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not submit update job to the operator.");
            job.State = "failed";
            job.Phase = "queue";
            job.Message = "Der Update-Operator konnte den Auftrag nicht entgegennehmen.";
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await PutAsync(JobKey, JsonSerializer.Serialize(job, JsonOptions), ct);
            return (null, "operator_unavailable");
        }
        return (job, null);
    }

    public static int Compare(string left, string right)
    {
        if (!Stable.IsMatch(left) || !Stable.IsMatch(right)) return 0;
        return SemVersion(left).CompareTo(SemVersion(right));
    }

    private static Version SemVersion(string value) => Version.Parse(value[1..]);
    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    private static DateTimeOffset? Next(DateTimeOffset? last, string interval) => interval switch
    {
        "daily" => (last ?? DateTimeOffset.UtcNow).AddDays(1),
        "weekly" => (last ?? DateTimeOffset.UtcNow).AddDays(7),
        "monthly" => (last ?? DateTimeOffset.UtcNow).AddMonths(1),
        _ => null
    };
    private static T? Deserialize<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task PutAsync(string key, string value, CancellationToken ct)
    {
        var setting = await db.SystemSettings.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (setting == null)
        {
            db.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task ImportOperatorStatusAsync(CancellationToken ct)
    {
        var dir = configuration["LearnPip:UpdateStatusPath"];
        if (string.IsNullOrWhiteSpace(dir)) return;
        var current = await db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == JobKey, ct);
        var job = Deserialize<UpdateJob>(current?.Value);
        if (job is null) return;
        var path = Path.Combine(dir, $"{job.Id:N}.json");
        if (!File.Exists(path)) return;
        try
        {
            var updated = JsonSerializer.Deserialize<UpdateJob>(await File.ReadAllTextAsync(path, ct), JsonOptions);
            if (updated is null || updated.Id != job.Id || updated.TargetVersion != job.TargetVersion) return;
            await PutAsync(JobKey, JsonSerializer.Serialize(updated, JsonOptions), ct);
            if (updated.State is "succeeded" or "failed")
            {
                var action = updated.State == "succeeded" ? "update.succeeded" : "update.failed";
                if (!await db.AdministrationAuditEvents.AnyAsync(x => x.Action == action &&
                    x.Target == $"update-job:{updated.Id}", ct))
                {
                    db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
                    {
                        ActorAccountId = updated.ActorAccountId,
                        Action = action,
                        Target = $"update-job:{updated.Id}",
                        PreviousValue = updated.FromVersion,
                        NewValue = updated.TargetVersion
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogWarning(ex, "Could not import update operator status.");
        }
    }
}
