namespace ImmichTagManager.Models;

public enum AssetItemStatus
{
    Queued,
    FetchingThumbnail,
    Analyzing,
    Saving,
    Done,
    Skipped,
    Failed
}

public class AssetProcessingItem
{
    public string AssetId { get; init; } = string.Empty;
    public AssetItemStatus Status { get; set; } = AssetItemStatus.Queued;
    public byte[]? ThumbnailBytes { get; set; }
    public string? Description { get; set; }
    public string[] Tags { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public bool DescriptionSaved { get; set; }
    public bool TagsSaved { get; set; }

    public string ThumbnailDataUrl =>
        ThumbnailBytes is { Length: > 0 }
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(ThumbnailBytes)}"
            : string.Empty;
}
