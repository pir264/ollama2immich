using System.Net;
using System.Text;
using ImmichTagManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImmichTagManager.Tests;

public class ImmichAssetServiceTests
{
    private static ImmichAssetService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var client = new HttpClient(new TestHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://test/")
        };
        return new ImmichAssetService(client, NullLogger<ImmichAssetService>.Instance);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetAllAssetsAsync_SinglePage_YieldsAssets()
    {
        const string json = """
            {
              "assets": {
                "items": [
                  {"id":"a1","type":"IMAGE","exifInfo":{"description":""}},
                  {"id":"a2","type":"IMAGE","exifInfo":null}
                ],
                "nextPage": null
              }
            }
            """;
        var service = CreateService(_ => Json(json));

        var assets = await service.GetAllAssetsAsync().ToListAsync();

        Assert.Equal(2, assets.Count);
        Assert.Equal("a1", assets[0].Id);
        Assert.Equal("IMAGE", assets[0].Type);
        Assert.Equal("a2", assets[1].Id);
    }

    [Fact]
    public async Task GetAllAssetsAsync_MultiplePages_PaginatesUntilDone()
    {
        int callCount = 0;
        var service = CreateService(_ =>
        {
            callCount++;
            var json = callCount == 1
                ? """{"assets":{"items":[{"id":"a1","type":"IMAGE","exifInfo":null}],"nextPage":"2"}}"""
                : """{"assets":{"items":[{"id":"a2","type":"IMAGE","exifInfo":null},{"id":"a3","type":"IMAGE","exifInfo":null}],"nextPage":null}}""";
            return Json(json);
        });

        var assets = await service.GetAllAssetsAsync().ToListAsync();

        Assert.Equal(3, assets.Count);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetAllAssetsAsync_EmptyPage_YieldsNothing()
    {
        const string json = """{"assets":{"items":[],"nextPage":null}}""";
        var service = CreateService(_ => Json(json));

        var assets = await service.GetAllAssetsAsync().ToListAsync();

        Assert.Empty(assets);
    }

    [Fact]
    public async Task GetThumbnailAsync_ReturnsBytes()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        HttpRequestMessage? captured = null;
        var service = CreateService(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedBytes)
            };
        });

        var result = await service.GetThumbnailAsync("asset-abc");

        Assert.Equal(expectedBytes, result);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured.Method);
        Assert.Contains("asset-abc", captured.RequestUri!.ToString());
        Assert.Contains("thumbnail", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateDescriptionAsync_SendsPutRequest()
    {
        HttpRequestMessage? captured = null;
        var service = CreateService(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await service.UpdateDescriptionAsync("asset-xyz", "een mooie foto");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Put, captured.Method);
        Assert.Contains("asset-xyz", captured.RequestUri!.ToString());
        var body = await captured.Content!.ReadAsStringAsync();
        Assert.Contains("een mooie foto", body);
    }
}

file static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }
}
