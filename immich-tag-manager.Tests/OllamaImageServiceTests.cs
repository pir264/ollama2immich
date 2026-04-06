using System.Net;
using System.Text;
using ImmichTagManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImmichTagManager.Tests;

public class OllamaImageServiceTests
{
    private static OllamaImageService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var client = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://test/")
        };
        return new OllamaImageService(client, "llava", "test prompt", "tag existing prompt", NullLogger<OllamaImageService>.Instance);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task AnalyzeImageAsync_ValidResponse_ReturnsDescriptionAndTags()
    {
        // The Ollama response wraps the actual JSON inside a "response" string field
        const string json = """{"response":"{\"description\":\"een foto van een hond\",\"tags\":[\"hond\",\"tuin\",\"zomer\"]}"}""";
        var service = CreateService(_ => Json(json));

        var (description, tags) = await service.AnalyzeImageAsync([1, 2, 3]);

        Assert.Equal("een foto van een hond", description);
        Assert.Equal(3, tags.Length);
        Assert.Contains("hond", tags);
        Assert.Contains("tuin", tags);
        Assert.Contains("zomer", tags);
    }

    [Fact]
    public async Task AnalyzeImageAsync_EmptyTags_ReturnsEmptyTagArray()
    {
        const string json = """{"response":"{\"description\":\"een foto\",\"tags\":[]}"}""";
        var service = CreateService(_ => Json(json));

        var (description, tags) = await service.AnalyzeImageAsync([1, 2, 3]);

        Assert.Equal("een foto", description);
        Assert.Empty(tags);
    }

    [Fact]
    public async Task AnalyzeImageAsync_HttpError_ThrowsException()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.AnalyzeImageAsync([1, 2, 3]));
    }

    [Fact]
    public async Task SelectTagsAsync_ValidResponse_ReturnsMatchedTags()
    {
        const string json = """{"response":"{\"tags\":[\"hond\",\"tuin\"]}"}""";
        var service = CreateService(_ => Json(json));

        var tags = await service.SelectTagsAsync([1, 2, 3], ["hond", "tuin", "auto", "fiets"]);

        Assert.Equal(2, tags.Length);
        Assert.Contains("hond", tags);
        Assert.Contains("tuin", tags);
    }

    [Fact]
    public async Task SelectTagsAsync_EmptyResponse_ReturnsEmptyArray()
    {
        const string json = """{"response":"{\"tags\":[]}"}""";
        var service = CreateService(_ => Json(json));

        var tags = await service.SelectTagsAsync([1, 2, 3], ["hond", "tuin"]);

        Assert.Empty(tags);
    }
}
