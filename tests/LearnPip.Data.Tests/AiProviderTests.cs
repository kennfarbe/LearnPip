using System.Net;
using System.Security.Cryptography;
using LearnPip.Api.Ai;
using Microsoft.Extensions.Configuration;

namespace LearnPip.Data.Tests;

public sealed class AiProviderTests
{
    private static IConfiguration Configure(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Default_is_off_and_cloud_requires_explicit_permission_and_https()
    {
        var config = Configure(new Dictionary<string, string?>
        {
            ["Ai:CloudEndpoint"] = "http://example.org/v1/chat/completions",
            ["Ai:CloudModel"] = "test",
            ["Ai:CloudKey"] = "secret"
        });
        Assert.True(AiPolicy.Describe("off", config, false).Available);
        Assert.False(AiPolicy.Describe("operator-cloud", config, false).Available);
        config = Configure(new Dictionary<string, string?>
        {
            ["Ai:AllowedModes"] = "off,operator-cloud",
            ["Ai:CloudEndpoint"] = "https://example.org/v1/chat/completions",
            ["Ai:CloudModel"] = "test",
            ["Ai:CloudKey"] = "secret",
            ["Ai:Quota:operator-cloud"] = "0"
        });
        Assert.False(AiPolicy.Describe("operator-cloud", config, false).Available);
    }

    [Fact]
    public void User_key_is_bound_to_account_and_never_exposed_by_mode_metadata()
    {
        var config = Configure(new Dictionary<string, string?>
        {
            ["Ai:KeyEncryptionKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["Ai:AllowedModes"] = "off,user-key",
            ["Ai:CloudEndpoint"] = "https://example.org/v1/chat/completions",
            ["Ai:CloudModel"] = "test"
        });
        var owner = Guid.NewGuid();
        var sealedKey = AiKeyVault.Seal("secret-key-0123456789", owner, config);
        Assert.DoesNotContain("secret-key", sealedKey);
        Assert.Equal("secret-key-0123456789", AiKeyVault.Open(sealedKey, owner, config));
        Assert.ThrowsAny<CryptographicException>(() => AiKeyVault.Open(sealedKey, Guid.NewGuid(), config));
        Assert.False(AiPolicy.Describe("user-key", config, false).Available);
        Assert.True(AiPolicy.Describe("user-key", config, true).Available);
        Assert.DoesNotContain("secret-key", AiPolicy.Describe("user-key", config, true).ToString());
    }

    [Fact]
    public async Task Provider_failure_does_not_trigger_another_provider()
    {
        var attempts = 0;
        using var client = new HttpClient(new StubHandler(() =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.BadGateway);
        }));
        var adapter = new ChatCompletionProvider(new Uri("https://example.org/v1/chat/completions"),
            "test", "secret", client);
        await Assert.ThrowsAsync<HttpRequestException>(() => adapter.GenerateAsync("test", default));
        Assert.Equal(1, attempts);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response());
    }
}
