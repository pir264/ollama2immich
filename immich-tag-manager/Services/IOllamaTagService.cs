using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public interface IOllamaTagService
{
    Task<OllamaTagAnalysis> AnalyzeTagsAsync(IList<ImmichTag> tags);
}
