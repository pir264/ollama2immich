using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public interface IImmichTagService
{
    Task<List<ImmichTag>> GetTagsAsync();
    Task<ImmichTag> CreateTagAsync(string name, string? parentId = null);
    Task UpdateTagAsync(string tagId, string? name = null, string? parentId = null);
    Task DeleteTagAsync(string tagId);
    Task<List<string>> GetAssetIdsByTagAsync(string tagId);
    Task AssignTagToAssetsAsync(string tagId, IList<string> assetIds);
}
