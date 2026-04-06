using System.Net;
using System.Text;
using System.Text.Json;
using ImmichTagManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImmichTagManager.Tests;

public class OllamaTagGeneratorServiceTests
{
    private static OllamaTagGeneratorService CreateService(string innerJson)
    {
        var ollamaResponse = JsonSerializer.Serialize(new { response = innerJson });
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ollamaResponse, Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        return new OllamaTagGeneratorService(client, "test-model", "prompt {0} diepte {1}", NullLogger<OllamaTagGeneratorService>.Instance);
    }

    [Fact]
    public async Task GenerateTagHierarchyAsync_ValidJson_ReturnsPaths()
    {
        const string innerJson = """{"tags":[{"path":["locatie","land","italië"]},{"path":["natuur","kleur","rood"]}]}""";
        var service = CreateService(innerJson);

        var result = await service.GenerateTagHierarchyAsync(100, 3);

        Assert.Equal(2, result.Count);
        Assert.Equal(["locatie", "land", "italië"], result[0]);
        Assert.Equal(["natuur", "kleur", "rood"], result[1]);
    }

    [Fact]
    public async Task GenerateTagHierarchyAsync_EmptyTags_ReturnsEmptyList()
    {
        const string innerJson = """{"tags":[]}""";
        var service = CreateService(innerJson);

        var result = await service.GenerateTagHierarchyAsync(100, 3);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateTagHierarchyAsync_InvalidJson_ThrowsException()
    {
        var service = CreateService("not-valid-json");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.GenerateTagHierarchyAsync(100, 3));
    }

    [Fact]
    public async Task GenerateTagHierarchyAsync_FiltersEmptyPaths()
    {
        const string innerJson = """{"tags":[{"path":["locatie","land","italië"]},{"path":[]}]}""";
        var service = CreateService(innerJson);

        var result = await service.GenerateTagHierarchyAsync(100, 3);

        Assert.Single(result);
        Assert.Equal(["locatie", "land", "italië"], result[0]);
    }
}
