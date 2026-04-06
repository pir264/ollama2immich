namespace ImmichTagManager.Services;

public interface IOllamaTagGeneratorService
{
    Task<List<string[]>> GenerateTagHierarchyAsync(int maxTags, int depth);
}
