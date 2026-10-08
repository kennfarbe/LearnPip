// <copyright file="CatalogSourceEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Security;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Erlaubt bewusst ausgelöste Downloads ausschließlich aus der Betreiberkonfiguration.</summary>
public static class CatalogSourceEndpoints
{
    /// <summary>Registriert die optionale neutrale Paketquellenliste; ohne Konfiguration bleibt sie leer.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die ergänzten Routen.</returns>
    public static IEndpointRouteBuilder MapCatalogSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/instance-catalogs/admin/sources").RequireAuthorization(ApiPolicies.Admin);
        group.MapGet("/", List);
        group.MapPost("/{id}/download", Download).RequireRateLimiting("content-write");
        return app;
    }

    private static IResult List(IConfiguration configuration, HttpContext context)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        var sources = configuration.GetSection("CatalogPackages:Sources").GetChildren()
            .Where(Valid).Select(source => new { Id = source.Key, Title = source["Title"], Sha256 = source["Sha256"] }).ToArray();
        return Results.Ok(new ApiResponse<object>(sources));
    }

    private static async Task<IResult> Download(string id, IConfiguration configuration, IHttpClientFactory clients, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var source = configuration.GetSection("CatalogPackages:Sources").GetChildren().SingleOrDefault(item => item.Key == id);
        if (source == null || !Valid(source))
        {
            return Results.NotFound();
        }

        try
        {
            // No caller-supplied URL, no redirects, no background requests; trusted operator configuration only.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            using var client = clients.CreateClient("catalog-package-source");
            using var response = await client.GetAsync(source["Url"], HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > CatalogPackageReader.MaxArchiveBytes)
            {
                return Invalid("Paketquelle ist nicht erreichbar, leitet um oder überschreitet die Größenbegrenzung.");
            }

            await using var input = await response.Content.ReadAsStreamAsync(token);
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int count;
            while ((count = await input.ReadAsync(bytes, token)) != 0)
            {
                if (buffer.Length + count > CatalogPackageReader.MaxArchiveBytes)
                {
                    return Invalid("Download überschreitet 25 MiB. Kein Import.");
                }

                await buffer.WriteAsync(bytes.AsMemory(0, count), token);
            }

            var archive = buffer.ToArray();
            if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(archive)), source["Sha256"], StringComparison.OrdinalIgnoreCase))
            {
                return Invalid("Prüfsumme der konfigurierten Paketfassung stimmt nicht überein. Kein Import.");
            }

            var package = CatalogPackageReader.Read(archive);
            CatalogPackageImporter.PrepareImages(package);
            return Results.File(archive, "application/zip", "learnpip-source-package.zip");
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException or TaskCanceledException)
        {
            return Invalid("Paket konnte nicht vollständig heruntergeladen und geprüft werden. Kein Import; bei Bedarf lokale ZIP-Datei verwenden.");
        }
    }

    private static bool Valid(IConfigurationSection source) =>
        !string.IsNullOrWhiteSpace(source["Title"]) && source["Title"]!.Length <= 256 &&
        Uri.TryCreate(source["Url"], UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 &&
        source["Sha256"] is { Length: 64 } hash && hash.All(Uri.IsHexDigit);

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["package"] = [message] });
}
