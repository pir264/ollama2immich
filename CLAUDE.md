# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Conventions

- This is a .NET project. Always target the latest .NET version specified in `global.json` or `.csproj` files.
- Prefer structured JSON outputs from LLM integrations rather than parsing free-text responses.

## Git

- Always place `.gitignore` in the repository root directory, not in subdirectories.
- When initializing Git repos, verify the `.gitignore` location before committing.

## LLM Integration

- When integrating with LLM APIs (e.g., Ollama), always use structured/JSON output mode rather than parsing free-text.
- Ensure prompts explicitly instruct the model to respond in English to avoid localization issues.

## Solution overview

The solution contains four projects:

| Project | Type | Purpose |
|---|---|---|
| `ollama2immich` | Console (.NET 10) | Processes photos via Ollama, writes descriptions and tags to Immich |
| `immich-tag-manager` | Blazor Server (.NET 10) | Interactive web UI for tag management and photo analysis |
| `immich-tag-manager.Tests` | xUnit (.NET 10) | Unit tests for the Blazor app |
| `immich-seeder` | Console (.NET 10) | Seeds Immich with test photos from Wikimedia |

## Commands

Run from the relevant project directory (where the `.csproj` lives).

```bash
# Build
dotnet build

# Run (requires appsettings.json to be configured)
dotnet run

# Publish a self-contained binary
dotnet publish -c Release

# Run tests (from immich-tag-manager.Tests/)
dotnet test
```

## ollama2immich — Configuration

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": "<required>"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llava",
    "Prompt": "...",
    "TagExistingPrompt": "..."
  },
  "Processing": {
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

`Immich:ApiKey` is the only required field. Settings can be overridden via environment variables (e.g. `Immich__ApiKey=...`).

## ollama2immich — Architecture

`Program.cs` wires up a generic host and drives the processing loop directly. Three modes:

- **Normal** (`dotnet run`) — generates descriptions and new tags per photo via `OllamaService.AnalyzeImageAsync`. Skips photos that already have a description.
- **Tag-existing** (`dotnet run -- --tag-existing`) — uses `OllamaService.SelectTagsAsync` to match photos against existing tags only. No new tags or descriptions are written.
- **Reset** (`dotnet run -- --reset`) — clears all descriptions and deletes all tags.

Flow (normal mode):
1. Fetch all Immich tags once and cache in `Dictionary<string, string>` (name → id).
2. Page through assets via `ImmichService.GetAllAssetsAsync` (async stream).
3. Concurrently process images via `SemaphoreSlim` (controlled by `Processing:ConcurrentAssets`).

Key details:
- **Structured output**: Ollama is called with a JSON schema (`format` field); responses are deserialized directly — no text parsing.
- **HTTP timeouts**: Immich client = 30 s; Ollama client = 5 min.
- The Ollama `HttpClient` is created manually (not via `AddHttpClient`) because `OllamaService` takes the model name and prompt as constructor parameters.

## immich-tag-manager — Architecture

Blazor Server app. Services registered in `Program.cs`:

| Interface | Purpose | Registration |
|---|---|---|
| `IImmichTagService` | Tag CRUD (GetTags, CreateTag, UpdateTag, DeleteTag, AssignTagToAssets) | `AddHttpClient<>`, 30 s timeout |
| `IImmichAssetService` | Asset pipeline (GetAllAssets, GetThumbnail, UpdateDescription) | `AddHttpClient<>`, 60 s timeout |
| `IOllamaTagService` | Text-based tag analysis (rename/merge/hierarchy proposals) | Manual singleton, 10 min timeout |
| `IOllamaTagGeneratorService` | Generate a tag hierarchy from scratch | Manual singleton, 10 min timeout |
| `IOllamaImageService` | Image analysis: `AnalyzeImageAsync` + `SelectTagsAsync` | Manual singleton, 5 min timeout |

Pages:
- `/` — analyse and reorganise existing Immich tags
- `/genereer-tags` — generate a tag hierarchy with Ollama
- `/analyseer-fotos` — process photos: normal mode (descriptions + new tags) or tag-existing mode (match existing tags only)

### Configuration (`immich-tag-manager/appsettings.json`)

```json
{
  "Immich": { "BaseUrl", "ApiKey" },
  "Ollama": {
    "BaseUrl", "Model",
    "TagPrompt", "TagGeneratorPrompt", "MaxGeneratedTags", "TagGeneratorDepth",
    "ImageModel", "ImagePrompt", "TagExistingPrompt",
    "ImageInstances": [{ "DisplayName", "BaseUrl", "Model" }]
  },
  "ImageAnalysis": { "FeedSize", "ConcurrentAssets", "PageSize" }
}
```

`ImageInstances` is optional. When empty, the default `BaseUrl`/`ImageModel` is used as a single fallback instance.

### UI patterns

- State enum per page (Idle → Loading/Running → Review/Stopped → Done)
- `InvokeAsync(StateHasChanged)` for UI updates from background tasks
- All UI text is Dutch
- CSS is defined inline in `App.razor` — no scoped CSS files
- Config defaults read via `IConfiguration` in `OnInitialized()`

### Photo analysis page (`/analyseer-fotos`)

- Three modes selectable via radio buttons: **Normaal**, **Bestaande tags**, **Reset**
- Rolling feed of last N photos (newest first), each showing thumbnail, status badge, instance name, description, tag chips, save indicators and error
- `AssetProcessingItem` class holds per-photo mutable state, including `InstanceDisplayName`
- `ConcurrentDictionary<string, string>` + `lock` block for tag-cache race conditions
- `IDisposable` on the component cancels processing on navigation

#### Multi-instance Ollama pool

- `OllamaImageInstances` (from `AppSettings`) drives a pool of `InstanceEntry` records built at `StartAsync()`
- Each `InstanceEntry` holds its own `OllamaImageService` (with `baseUrlOverride`/`modelOverride`), a dedicated `SemaphoreSlim(_concurrentAssets)`, and an owned `HttpClient` (disposed on component teardown)
- Photos are assigned round-robin (`assetIndex % instances.Count`) before the semaphore wait
- Total concurrency = N instances × `ConcurrentAssets`
- When `OllamaImageInstances` is empty, falls back to the injected `IOllamaImageService` singleton with display name "Standaard"
- Per-instance processed count tracked in `Dictionary<string, int> _instanceStats`; shown as a table at end/stop
- `OllamaImageService` accepts optional `baseUrlOverride` and `modelOverride` constructor params; prompts are always read live from `IAppSettingsService`

## Testing

Tests live in `immich-tag-manager.Tests/`. Patterns:
- **xUnit** with `[Fact]`
- **`TestHttpMessageHandler`** (custom) for HTTP mocking — not Moq or NSubstitute
- **`NullLogger<T>.Instance`** for logger dependencies
- **No FluentAssertions** — use `Assert.*` from xUnit only

Standard factory pattern:
```csharp
private static XxxService CreateService(Func<HttpRequestMessage, HttpResponseMessage> handler)
{
    var client = new HttpClient(new TestHttpMessageHandler(handler))
    {
        BaseAddress = new Uri("http://test/")
    };
    return new XxxService(client, NullLogger<XxxService>.Instance);
}
```
