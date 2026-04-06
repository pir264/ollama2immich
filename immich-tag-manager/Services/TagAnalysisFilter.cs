using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public static class TagAnalysisFilter
{
    public static OllamaTagAnalysis Filter(OllamaTagAnalysis analysis, IEnumerable<ImmichTag> allTags)
    {
        var existing = allTags.Select(t => t.Name.ToLowerInvariant()).ToHashSet();

        var renames = analysis.Renames
            .Where(r => existing.Contains(r.From.ToLowerInvariant())
                     && !r.From.Equals(r.To, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var merges = analysis.Merges
            .Select(m => m with
            {
                Discard = m.Discard
                    .Where(d => !d.Equals(m.Keep, StringComparison.OrdinalIgnoreCase))
                    .ToList()
            })
            .Where(m => m.Discard.Count > 0)
            .ToList();

        var parents = analysis.Parents
            .Select(p => p with
            {
                Children = p.Children
                    .Where(c => existing.Contains(c.ToLowerInvariant()))
                    .ToList()
            })
            .Where(p => p.Children.Count > 0)
            .ToList();

        return analysis with { Renames = renames, Merges = merges, Parents = parents };
    }
}
