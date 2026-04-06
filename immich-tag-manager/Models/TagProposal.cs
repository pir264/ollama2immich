using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record RenameProposal(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To
);

public record MergeProposal(
    [property: JsonPropertyName("keep")] string Keep,
    [property: JsonPropertyName("discard")] List<string> Discard
);

public record ParentProposal(
    [property: JsonPropertyName("parent")] string Parent,
    [property: JsonPropertyName("children")] List<string> Children,
    [property: JsonPropertyName("is_new")] bool IsNew
);
