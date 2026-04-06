using System.Net;
using System.Text;
using System.Text.Json;
using ImmichTagManager.Models;
using ImmichTagManager.Services;
using Xunit;

namespace ImmichTagManager.Tests;

public class OllamaTagServiceTests
{
    private static OllamaTagService CreateService(string innerJson)
    {
        var ollamaResponse = JsonSerializer.Serialize(new { response = innerJson });
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ollamaResponse, Encoding.UTF8, "application/json")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        return new OllamaTagService(client, "test-model", "test-prompt");
    }

    [Fact]
    public async Task AnalyzeTagsAsync_ValidJson_ReturnsRenames()
    {
        const string innerJson = """{"renames":[{"from":"bomen","to":"boom"}],"merges":[],"parents":[]}""";
        var service = CreateService(innerJson);

        var result = await service.AnalyzeTagsAsync([new ImmichTag("id1", "bomen", null)]);

        Assert.Single(result.Renames);
        Assert.Equal("bomen", result.Renames[0].From);
        Assert.Equal("boom", result.Renames[0].To);
        Assert.Empty(result.Merges);
        Assert.Empty(result.Parents);
    }

    [Fact]
    public async Task AnalyzeTagsAsync_ValidJson_ReturnsMerges()
    {
        const string innerJson = """{"renames":[],"merges":[{"keep":"trein","discard":["treinen","trains"]}],"parents":[]}""";
        var service = CreateService(innerJson);

        var result = await service.AnalyzeTagsAsync([]);

        Assert.Single(result.Merges);
        Assert.Equal("trein", result.Merges[0].Keep);
        Assert.Equal(2, result.Merges[0].Discard.Count);
        Assert.Contains("treinen", result.Merges[0].Discard);
    }

    [Fact]
    public async Task AnalyzeTagsAsync_ValidJson_ReturnsParents()
    {
        const string innerJson = """{"renames":[],"merges":[],"parents":[{"parent":"natuur","children":["boom","waterval"],"is_new":true}]}""";
        var service = CreateService(innerJson);

        var result = await service.AnalyzeTagsAsync([]);

        Assert.Single(result.Parents);
        Assert.Equal("natuur", result.Parents[0].Parent);
        Assert.True(result.Parents[0].IsNew);
        Assert.Equal(2, result.Parents[0].Children.Count);
    }

    [Fact]
    public async Task AnalyzeTagsAsync_EmptyLists_ReturnsEmptyCollections()
    {
        const string innerJson = """{"renames":[],"merges":[],"parents":[]}""";
        var service = CreateService(innerJson);

        var result = await service.AnalyzeTagsAsync([]);

        Assert.Empty(result.Renames);
        Assert.Empty(result.Merges);
        Assert.Empty(result.Parents);
    }

    [Fact]
    public async Task AnalyzeTagsAsync_InvalidJson_ThrowsException()
    {
        var service = CreateService("not-valid-json");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.AnalyzeTagsAsync([]));
    }
}
