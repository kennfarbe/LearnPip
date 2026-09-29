using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
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

    [Fact]
    public async Task Photo_request_sends_the_selected_image_and_no_extra_account_data()
    {
        string? body = null;
        using var client = new HttpClient(new PhotoHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"{}\"}}]}")
            };
        }));
        var adapter = new ChatCompletionProvider(new Uri("https://example.org/v1/chat/completions"),
            "vision-test", "secret", client);
        await adapter.AnalyzeImageAsync("Recognize this", [1, 2, 3], "image/jpeg", default);
        using var json = JsonDocument.Parse(body!);
        var message = json.RootElement.GetProperty("messages")[0];
        Assert.Equal("Recognize this", message.GetProperty("content")[0].GetProperty("text").GetString());
        Assert.Equal("data:image/jpeg;base64,AQID", message.GetProperty("content")[1]
            .GetProperty("image_url").GetProperty("url").GetString());
        Assert.DoesNotContain("secret", body);
    }

    [Fact]
    public void Photo_review_rejects_incomplete_output_and_never_claims_mathematical_verification()
    {
        var mediaId = Guid.NewGuid();
        Assert.Null(PhotoDraftParser.Parse("{}", mediaId, "operator-local", null));
        const string output = """
            {"detectedText":"2 + 2 = ?","questionText":"2 + 2 = ?","subject":"Mathe",
             "topic":"Addition","formula":"2+2","drawingDescription":null,
             "answers":["3","4"],"suggestedCorrectIndex":1,"computedSolution":"4",
             "steps":["2+2=4"],"referenceSolution":null,"uncertainties":[]}
            """;
        var review = PhotoDraftParser.Parse(output, mediaId, "operator-local", "2 + 2");
        Assert.NotNull(review);
        Assert.Equal("different-text", review.Comparison);
        Assert.Contains("nicht bewiesen", review.ComparisonExplanation);
        Assert.Contains(review.Recognition.Uncertainties, item => item.Contains("ungeprüft"));
        Assert.Equal("2 + 2", review.Recognition.ReferenceSolution);
        Assert.Null(PhotoDraftParser.Parse(output.Replace("\"suggestedCorrectIndex\":1",
            "\"suggestedCorrectIndex\":7"), mediaId, "operator-local", null));
    }

    private sealed class StubHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response());
    }

    private sealed class PhotoHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => response(request);
    }
}
