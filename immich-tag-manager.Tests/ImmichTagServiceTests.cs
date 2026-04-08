using System.Net;
using System.Text;
using ImmichTagManager.Models;
using ImmichTagManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImmichTagManager.Tests;

public class ImmichTagServiceTests
{
    private static ImmichTagService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var settings = new FakeAppSettingsService(new AppSettings { ImmichBaseUrl = "http://test", ImmichApiKey = "test-key" });
        var client = new HttpClient(new TestHttpMessageHandler(handler));
        return new ImmichTagService(client, settings, NullLogger<ImmichTagService>.Instance);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetTagsAsync_DeserializesTagList()
    {
        const string json = """[{"id":"abc","name":"boom","parentId":null}]""";
        var service = CreateService(_ => Json(json));

        var tags = await service.GetTagsAsync();

        Assert.Single(tags);
        Assert.Equal("abc", tags[0].Id);
        Assert.Equal("boom", tags[0].Name);
        Assert.Null(tags[0].ParentId);
    }

    [Fact]
    public async Task GetTagsAsync_EmptyArray_ReturnsEmptyList()
    {
        var service = CreateService(_ => Json("[]"));

        var tags = await service.GetTagsAsync();

        Assert.Empty(tags);
    }

    [Fact]
    public async Task GetAssetIdsByTagAsync_SinglePage_ReturnsIds()
    {
        const string json = """{"assets":{"items":[{"id":"a1"},{"id":"a2"}],"nextPage":null}}""";
        var service = CreateService(_ => Json(json));

        var ids = await service.GetAssetIdsByTagAsync("tag1");

        Assert.Equal(2, ids.Count);
        Assert.Contains("a1", ids);
        Assert.Contains("a2", ids);
    }

    [Fact]
    public async Task GetAssetIdsByTagAsync_MultiplePages_PaginatesUntilDone()
    {
        int callCount = 0;
        var service = CreateService(_ =>
        {
            callCount++;
            var json = callCount == 1
                ? """{"assets":{"items":[{"id":"a1"}],"nextPage":"2"}}"""
                : """{"assets":{"items":[{"id":"a2"},{"id":"a3"}],"nextPage":null}}""";
            return Json(json);
        });

        var ids = await service.GetAssetIdsByTagAsync("tag1");

        Assert.Equal(3, ids.Count);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetAssetIdsByTagAsync_EmptyResult_ReturnsEmptyList()
    {
        const string json = """{"assets":{"items":[],"nextPage":null}}""";
        var service = CreateService(_ => Json(json));

        var ids = await service.GetAssetIdsByTagAsync("tag1");

        Assert.Empty(ids);
    }

    [Fact]
    public async Task CreateTagAsync_WithoutParent_SendsNameOnly()
    {
        HttpRequestMessage? captured = null;
        var service = CreateService(req =>
        {
            captured = req;
            return Json("""{"id":"new-id","name":"natuur","parentId":null}""");
        });

        var tag = await service.CreateTagAsync("natuur");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("api/tags", captured.RequestUri!.PathAndQuery.TrimStart('/'));
        Assert.Equal("new-id", tag.Id);
    }

    [Fact]
    public async Task DeleteTagAsync_SendsDeleteRequest()
    {
        HttpRequestMessage? captured = null;
        var service = CreateService(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        await service.DeleteTagAsync("tag-id");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Delete, captured.Method);
        Assert.Contains("tag-id", captured.RequestUri!.ToString());
    }
}
