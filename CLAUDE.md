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

## What this project does

`ollama2immich` is a .NET 10 console application that iterates over all image assets in an [Immich](https://immich.app/) photo library, sends each thumbnail to a local [Ollama](https://ollama.ai/) vision model (default: `llava`), and writes the generated description and keyword tags back to Immich.

## Commands

All commands run from the `ollama2immich/` project directory (where the `.csproj` lives).

```bash
# Build
dotnet build

# Run (requires appsettings.json to be configured)
dotnet run

# Publish a self-contained binary
dotnet publish -c Release
```

There are no tests in this project.

## Configuration

Copy `appsettings.json` and set the required fields before running:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": "<required>"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llava"
  },
  "Processing": {
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

`Immich:ApiKey` is the only required field — the app exits immediately if it is empty. Settings can also be overridden via environment variables (e.g. `Immich__ApiKey=...`).

## Architecture

The entry point (`Program.cs`) wires up a generic host with two typed HTTP clients and drives the main processing loop directly (no hosted service or worker class). The flow is:

1. **Fetch all Immich tags** once at startup and cache them in a `Dictionary<string, string>` (name → id) to avoid redundant API calls.
2. **Page through assets** via `ImmichService.GetAllAssetsAsync` (async stream). Skip non-`IMAGE` types and assets that already have a description.
3. **Concurrently process** images via a `SemaphoreSlim` (controlled by `Processing:ConcurrentAssets`). For each asset:
   - Download the preview thumbnail (`ImmichService.GetThumbnailAsync`).
   - Send it base64-encoded to Ollama (`OllamaService.AnalyzeImageAsync`).
   - Write the description back to Immich (`ImmichService.UpdateDescriptionAsync`).
   - Create any new tags and assign them to the asset.

### Key design details

- **Ollama prompt** is a fixed constant in `OllamaService` that instructs the model to respond in a structured `DESCRIPTION: ... / TAGS: ...` format. `ParseResponse` enforces this format and throws `FormatException` if `DESCRIPTION:` is missing.
- **Tag deduplication** happens in-memory using the pre-fetched `existingTags` dictionary. The dictionary is mutated from concurrent tasks without a lock — this is a potential race condition if two tasks create the same new tag simultaneously.
- **HTTP timeouts**: Immich client = 30 s; Ollama client = 5 min (vision inference is slow).
- The Ollama `HttpClient` is created manually (not via `AddHttpClient`) because `OllamaService` takes the model name as a constructor parameter alongside the client.
