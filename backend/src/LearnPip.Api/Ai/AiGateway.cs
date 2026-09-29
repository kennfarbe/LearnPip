using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public sealed record AiModeInfo(string Mode, bool Available, string Recipient,
    string DataShared, int DailyQuota, int MaxInputBytes, int MaxImageBytes,
    bool NeedsUserKey);

public static class AiPolicy
{
    public const string DisclosureVersion = "ai-photo-v2";
    public static readonly string[] Modes = ["off", "operator-cloud", "operator-local", "user-key"];

    public static AiModeInfo Describe(string mode, IConfiguration config, bool hasUserKey)
    {
        var allowed = (config["Ai:AllowedModes"] ?? "off").Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(mode, StringComparer.Ordinal);
        var local = mode == "operator-local";
        var endpoint = local ? config["Ai:LocalEndpoint"] : config["Ai:CloudEndpoint"];
        var uriValid = Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
            (local ? LocalUri(uri) : uri.Scheme == Uri.UriSchemeHttps);
        var model = local ? config["Ai:LocalModel"] : config["Ai:CloudModel"];
        var keyReady = mode switch
        {
            "operator-cloud" => !string.IsNullOrWhiteSpace(config["Ai:CloudKey"]),
            "user-key" => hasUserKey && AiKeyVault.Available(config),
            _ => true
        };
        var quota = int.TryParse(config[$"Ai:Quota:{mode}"], out var configured) ? configured :
            mode == "operator-cloud" ? 10 : mode == "operator-local" ? 50 : 20;
        quota = Math.Clamp(quota, 0, 1000);
        var max = int.TryParse(config["Ai:MaxInputBytes"], out var configuredMax) ?
            configuredMax : 8192;
        max = Math.Clamp(max, 1, 32768);
        var imageMax = int.TryParse(config["Ai:MaxImageBytes"], out var configuredImageMax) ?
            configuredImageMax : 2 * 1024 * 1024;
        imageMax = Math.Clamp(imageMax, 1, 5 * 1024 * 1024);
        return mode == "off" ? new AiModeInfo(mode, true, "Kein Anbieter",
            "Keine Übermittlung. Manuelles Lernen bleibt verfügbar.", 0, max, imageMax, false) :
            new AiModeInfo(mode, allowed && uriValid && !string.IsNullOrWhiteSpace(model) &&
                keyReady && quota > 0,
                uriValid ? uri!.GetLeftPart(UriPartial.Authority) : "Nicht konfiguriert",
                local ? "Ausdrücklich gewählter Text oder Foto an den lokalen Dienst des Betreibers." :
                    "Ausdrücklich gewählter Text oder Foto an den Cloud-Anbieter; " +
                    (mode == "user-key" ? "der eigene API-Schlüssel wird mitgesendet." :
                        "der Betreiber trägt den API-Schlüssel."),
                quota, max, imageMax, mode == "user-key");
    }

    private static bool LocalUri(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        if (uri.Host is "localhost" or "127.0.0.1" or "::1") return true;
        if (!uri.Host.Contains('.') && uri.Host.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-')) return true;
        if (!IPAddress.TryParse(uri.Host, out var address)) return false;
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && (bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 ||
            bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
    }
}

public static class AiKeyVault
{
    private static byte[] Key(IConfiguration config)
    {
        var encoded = config["Ai:KeyEncryptionKey"] ?? "";
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length != 32) throw new CryptographicException("Invalid AI encryption key.");
        return bytes;
    }

    public static bool Available(IConfiguration config)
    {
        try { return Key(config).Length == 32; }
        catch (FormatException) { return false; }
        catch (CryptographicException) { return false; }
    }

    public static string Seal(string secret, Guid accountId, IConfiguration config)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(secret);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key(config), 16);
        aes.Encrypt(nonce, plain, cipher, tag, accountId.ToByteArray());
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public static string Open(string sealedValue, Guid accountId, IConfiguration config)
    {
        var data = Convert.FromBase64String(sealedValue);
        if (data.Length < 29) throw new CryptographicException("Invalid encrypted key.");
        var plain = new byte[data.Length - 28];
        using var aes = new AesGcm(Key(config), 16);
        aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), plain,
            accountId.ToByteArray());
        return Encoding.UTF8.GetString(plain);
    }
}

public interface IAiProvider
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
    Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken);
}

public sealed class DisabledAiProvider : IAiProvider
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("AI is disabled.");
    public Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken) => throw new InvalidOperationException("AI is disabled.");
}

public sealed class ChatCompletionProvider(Uri endpoint, string model, string? key,
    HttpClient client) : IAiProvider
{
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
        => await CompleteAsync(new { role = "user", content = (object)prompt }, 512,
            cancellationToken);

    public Task<string> AnalyzeImageAsync(string instruction, byte[] image, string mediaType,
        CancellationToken cancellationToken)
    {
        object[] content =
        [
            new { type = "text", text = instruction },
            new { type = "image_url", image_url = new
                { url = $"data:{mediaType};base64,{Convert.ToBase64String(image)}" } }
        ];
        return CompleteAsync(new { role = "user", content = (object)content }, 2048,
            cancellationToken);
    }

    private async Task<string> CompleteAsync(object message, int outputTokens,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (key != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { message },
            max_tokens = outputTokens
        }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("AI provider failed.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) != 0)
        {
            if (buffer.Length + count > 65536) throw new InvalidDataException("AI response too large.");
            buffer.Write(chunk, 0, count);
        }
        using var json = JsonDocument.Parse(buffer.ToArray());
        var text = json.RootElement.GetProperty("choices")[0].GetProperty("message")
            .GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("Empty AI response.");
        return text.Length > 16000 ? text[..16000] : text;
    }
}

public sealed class AiGateway : IDisposable
{
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public IAiProvider Resolve(string mode, IConfiguration config, string? userKey) =>
        mode == "off" ? new DisabledAiProvider() : new ChatCompletionProvider(
            new Uri(config[mode == "operator-local" ? "Ai:LocalEndpoint" : "Ai:CloudEndpoint"]!),
            config[mode == "operator-local" ? "Ai:LocalModel" : "Ai:CloudModel"]!, mode switch
            {
                "operator-cloud" => config["Ai:CloudKey"],
                "user-key" => userKey,
                _ => null
            }, client);

    public void Dispose() => client.Dispose();
}
