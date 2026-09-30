using System.Net;
using System.Net.Mail;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Worker;

public sealed class LearningReminderService(LearnPipDbContext db, IConfiguration config)
{
    public async Task<int> RunOnceAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config["Mail:Host"]) ||
            string.IsNullOrWhiteSpace(config["Mail:From"]) ||
            string.IsNullOrWhiteSpace(config["Mail:Username"]) ||
            string.IsNullOrWhiteSpace(config["Mail:Password"])) return 0;
        var candidates = await db.ReminderPreferences.AsNoTracking()
            .Join(db.Accounts.AsNoTracking(), preference => preference.AccountId,
                account => account.Id, (preference, account) => new { preference, account })
            .Where(item => item.preference.Enabled && item.account.DeletedAtUtc == null &&
                item.account.DisabledAtUtc == null &&
                item.account.LastActivityAtUtc <= now.AddDays(-1) &&
                (item.preference.LastNotifiedActivityAtUtc == null ||
                 item.preference.LastNotifiedActivityAtUtc < item.account.LastActivityAtUtc))
            .Select(item => new { item.preference.AccountId, item.preference.IntervalDays,
                item.preference.QuietStartMinute, item.preference.QuietEndMinute,
                item.preference.TimeZoneId, item.account.LastActivityAtUtc })
            .ToListAsync(ct);
        var sent = 0;
        foreach (var item in candidates)
        {
            if (item.LastActivityAtUtc.AddDays(item.IntervalDays) > now) continue;
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(item.TimeZoneId); }
            catch (TimeZoneNotFoundException) { continue; }
            catch (InvalidTimeZoneException) { continue; }
            var local = TimeZoneInfo.ConvertTime(now, zone);
            var minute = local.Hour * 60 + local.Minute;
            var quiet = item.QuietStartMinute < item.QuietEndMinute
                ? minute >= item.QuietStartMinute && minute < item.QuietEndMinute
                : minute >= item.QuietStartMinute || minute < item.QuietEndMinute;
            if (quiet) continue;
            var email = await db.ExternalIdentities.AsNoTracking()
                .Where(identity => identity.AccountId == item.AccountId && identity.Provider == "email")
                .Select(identity => identity.Subject).FirstOrDefaultAsync(ct);
            if (email == null) continue;
            // Claim exactly once for this activity period, even if SMTP or the worker crashes.
            var claimed = await db.ReminderPreferences.Where(preference =>
                preference.AccountId == item.AccountId && preference.Enabled &&
                (preference.LastNotifiedActivityAtUtc == null ||
                 preference.LastNotifiedActivityAtUtc < item.LastActivityAtUtc) &&
                db.Accounts.Any(account => account.Id == preference.AccountId &&
                    account.DeletedAtUtc == null && account.DisabledAtUtc == null &&
                    account.LastActivityAtUtc == item.LastActivityAtUtc))
                .ExecuteUpdateAsync(setters => setters.SetProperty(preference =>
                    preference.LastNotifiedActivityAtUtc, item.LastActivityAtUtc), ct);
            if (claimed != 1) continue;
            try
            {
                using var client = new SmtpClient(config["Mail:Host"]!,
                    int.TryParse(config["Mail:Port"], out var port) ? port : 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(config["Mail:Username"], config["Mail:Password"])
                };
                using var message = new MailMessage(config["Mail:From"]!, email)
                {
                    Subject = "LearnPip: Deine Lernerinnerung",
                    Body = "Du hast deine freiwillige Lernerinnerung aktiviert. Wenn du Zeit hast, besuche LearnPip. " +
                           "Deine Erinnerungen kannst du jederzeit in den Einstellungen ausschalten."
                };
                await client.SendMailAsync(message, ct);
                await db.ReminderPreferences.Where(preference => preference.AccountId == item.AccountId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(preference => preference.LastSentAtUtc, now), ct);
                sent++;
            }
            catch (Exception exception) when (!ct.IsCancellationRequested)
            {
                // Keep the claim: automatic retries could send a duplicate if SMTP accepted the email.
                Console.Error.WriteLine($"Learning reminder delivery failed for account {item.AccountId}: {exception.Message}");
            }
        }
        return sent;
    }
}
