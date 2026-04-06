using ImmichTagManager.Models;
using ImmichTagManager.Services;
using Xunit;

namespace ImmichTagManager.Tests;

public class TagAnalysisFilterTests
{
    private static ImmichTag Tag(string name) => new(Guid.NewGuid().ToString(), name, null);

    private static OllamaTagAnalysis EmptyAnalysis() =>
        new([], [], []);

    // --- Renames ---

    [Fact]
    public void Filter_RemovesSelfRename()
    {
        var analysis = EmptyAnalysis() with
        {
            Renames = [new RenameProposal("zon", "zon")]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("zon")]);

        Assert.Empty(result.Renames);
    }

    [Fact]
    public void Filter_RemovesSelfRename_CaseInsensitive()
    {
        var analysis = EmptyAnalysis() with
        {
            Renames = [new RenameProposal("Zon", "zon")]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("Zon")]);

        Assert.Empty(result.Renames);
    }

    [Fact]
    public void Filter_RemovesRenameWhenSourceDoesNotExist()
    {
        var analysis = EmptyAnalysis() with
        {
            Renames = [new RenameProposal("bos", "forest")]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("boom")]);

        Assert.Empty(result.Renames);
    }

    [Fact]
    public void Filter_KeepsValidRename()
    {
        var analysis = EmptyAnalysis() with
        {
            Renames = [new RenameProposal("bomen", "boom")]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("bomen")]);

        Assert.Single(result.Renames);
        Assert.Equal("bomen", result.Renames[0].From);
        Assert.Equal("boom", result.Renames[0].To);
    }

    // --- Merges ---

    [Fact]
    public void Filter_RemovesSelfDiscard()
    {
        var analysis = EmptyAnalysis() with
        {
            Merges = [new MergeProposal("bos", ["bos"])]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("bos")]);

        Assert.Empty(result.Merges);
    }

    [Fact]
    public void Filter_RemovesSelfDiscard_CaseInsensitive()
    {
        var analysis = EmptyAnalysis() with
        {
            Merges = [new MergeProposal("Bos", ["bos"])]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("Bos")]);

        Assert.Empty(result.Merges);
    }

    [Fact]
    public void Filter_RemovesMergeWhenAllDiscardsAreSelf()
    {
        var analysis = EmptyAnalysis() with
        {
            Merges = [new MergeProposal("boom", ["boom", "Boom"])]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("boom")]);

        Assert.Empty(result.Merges);
    }

    [Fact]
    public void Filter_KeepsValidMerge()
    {
        var analysis = EmptyAnalysis() with
        {
            Merges = [new MergeProposal("boom", ["bomen", "boomtje"])]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("boom"), Tag("bomen"), Tag("boomtje")]);

        Assert.Single(result.Merges);
        Assert.Equal("boom", result.Merges[0].Keep);
        Assert.Equal(2, result.Merges[0].Discard.Count);
    }

    // --- Parents ---

    [Fact]
    public void Filter_RemovesNonExistentChild()
    {
        var analysis = EmptyAnalysis() with
        {
            Parents = [new ParentProposal("natuur", ["bos", "bestaat-niet"], false)]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("natuur"), Tag("bos")]);

        Assert.Single(result.Parents);
        Assert.Single(result.Parents[0].Children);
        Assert.Equal("bos", result.Parents[0].Children[0]);
    }

    [Fact]
    public void Filter_RemovesParentWhenAllChildrenMissing()
    {
        var analysis = EmptyAnalysis() with
        {
            Parents = [new ParentProposal("natuur", ["bestaat-niet"], false)]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("natuur")]);

        Assert.Empty(result.Parents);
    }

    [Fact]
    public void Filter_KeepsNewParentWithExistingChildren()
    {
        var analysis = EmptyAnalysis() with
        {
            Parents = [new ParentProposal("dieren", ["hond", "kat"], true)]
        };

        var result = TagAnalysisFilter.Filter(analysis, [Tag("hond"), Tag("kat")]);

        Assert.Single(result.Parents);
        Assert.Equal("dieren", result.Parents[0].Parent);
        Assert.Equal(2, result.Parents[0].Children.Count);
        Assert.True(result.Parents[0].IsNew);
    }
}
