using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaTagAnalysis(
    [property: JsonPropertyName("renames")] List<RenameProposal> Renames,
    [property: JsonPropertyName("merges")] List<MergeProposal> Merges,
    [property: JsonPropertyName("parents")] List<ParentProposal> Parents
);
