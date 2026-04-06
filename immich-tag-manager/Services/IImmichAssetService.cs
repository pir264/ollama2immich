using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public interface IImmichAssetService
{
    IAsyncEnumerable<ImmichAsset> GetAllAssetsAsync(int pageSize = 50, CancellationToken cancellationToken = default);
    Task<byte[]> GetThumbnailAsync(string assetId);
    Task UpdateDescriptionAsync(string assetId, string description);
}
