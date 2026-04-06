# ollama2immich

> [🇳🇱 Nederlandse versie](README.md)

A collection of .NET 10 tools to automatically enrich and organise an [Immich](https://immich.app/) photo library using a local [Ollama](https://ollama.com/) language model.

## Projects

| Project | Type | Purpose |
|---|---|---|
| [`ollama2immich`](#ollama2immich-1) | Console | Generates descriptions and tags for each photo using a vision model |
| [`immich-tag-manager`](#immich-tag-manager-1) | Blazor Server (web) | Organises tags and analyses photos via a web UI |
| [`immich-seeder`](#immich-seeder-1) | Console | Populates Immich with test photos from Wikimedia |

All tools run fully locally — no data is sent to external services.

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A running [Immich](https://immich.app/) instance
- A running [Ollama](https://ollama.com/) instance

---

## ollama2immich

Iterates over all photos in the Immich library, sends each thumbnail to an Ollama vision model and writes the generated description and keyword tags back to Immich.

### How it works

The tool has three modes:

**Normal mode** (`dotnet run`)
1. All existing Immich tags are fetched once and cached.
2. Assets are fetched page by page; non-images and already-described photos are skipped.
3. Per photo: fetch thumbnail → analyse via Ollama (structured output) → write back description and tags.
4. New tags are created if they do not yet exist.

**Tag-existing mode** (`dotnet run -- --tag-existing`)
1. All existing Immich tags are fetched.
2. For each photo, Ollama receives the thumbnail together with the list of existing tag names and selects which tags apply.
3. Only matching existing tags are linked — no new tags are created and descriptions are not changed.
4. Useful when the tag hierarchy has already been built (via `immich-tag-manager`) and each photo still needs to be linked to the right tags.

**Reset mode** (`dotnet run -- --reset`)
Clears all descriptions and deletes all tags so processing can start fresh.

### Configuration

Create `ollama2immich/appsettings.Local.json` (listed in `.gitignore`):

```json
{
  "Immich": {
    "ApiKey": "<your-api-key>"
  }
}
```

All available settings:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llava",
    "Prompt": "Examine this photo carefully. ...",
    "TagExistingPrompt": "Examine this photo carefully. Below is a list of existing tags. ..."
  },
  "Processing": {
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

The Immich API key requires at least these scopes:

| Scope | Used for |
|---|---|
| `asset.read` | Fetching metadata and thumbnails |
| `asset.view` | Downloading thumbnails |
| `asset.update` | Writing back descriptions |
| `tag.read` | Fetching existing tags |
| `tag.create` | Creating new tags |

### Running

```bash
cd ollama2immich

# Process photos (generate descriptions + new tags)
dotnet run

# Link existing tags to photos (no new tags or descriptions)
dotnet run -- --tag-existing

# Clear all descriptions and tags (useful for testing)
dotnet run -- --reset
```

---

## immich-tag-manager

A Blazor Server web application for organising tags and analysing photos. Accessible via the navigation bar in the browser.

### Manage tags (`/`)

1. **Analyse** — the app fetches all tags from Immich and sends them to Ollama.
2. **Proposals** — Ollama suggests three types of changes:
   - **Renames** — normalise tags to singular and lowercase (`Trees` → `tree`)
   - **Merges** — combine synonyms and duplicates (`trains`, `train` → `train`); all photos of the discarded tag are moved to the kept tag
   - **Hierarchy** — suggest abstract parent tags and assign children (`tree`, `waterfall` → parent `nature`); new parent tags are created if needed
3. **Review** — each suggestion appears as a checkbox; the user selects what to apply.
4. **Apply** — changes are applied to Immich in order, with a live progress log in the browser.

### Generate hierarchy (`/genereer-tags`)

Let Ollama generate a broadly applicable tag hierarchy for a photo library — without analysing any photos. Configurable: maximum number of tags (default 100) and hierarchy depth (default 3). All tags are singular and lowercase. Example paths: `location → country → italy`, `nature → colour → red`.

1. **Generate** — Ollama reasons over categories such as location, nature, people, buildings, food, transport, seasons, colours and activities.
2. **Review** — each tag path appears as a checkbox; select what is relevant.
3. **Apply** — selected paths are saved as nested tags in Immich. Existing tags are reused.

### Photo analysis (`/analyseer-fotos`)

Runs the image processing pipeline from `ollama2immich` directly in the browser, with a live feed of the last N photos being processed.

Two modes (selectable via radio buttons):

**Normal** — Ollama generates a description and new tags per photo. Photos that already have a description are skipped.

**Tag-existing** — Ollama selects which existing Immich tags apply to each photo. No new tags or descriptions are written. Useful when the tag hierarchy has already been built.

Each photo card in the feed shows:
- Thumbnail
- Status (Queued / Fetching thumbnail / Analysing / Saving / Done / Error)
- Generated description and tags
- Confirmation once description and/or tags have been saved to Immich
- Error message if something goes wrong (other photos continue processing)

### Configuration

Create `immich-tag-manager/appsettings.Local.json`:

```json
{
  "Immich": {
    "ApiKey": "<your-api-key>"
  }
}
```

All available settings:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "gemma4",
    "TagPrompt": "You receive a list of Immich photo tags. ...",
    "MaxGeneratedTags": 100,
    "TagGeneratorDepth": 3,
    "TagGeneratorPrompt": "You are an expert in organising photo libraries. ...",
    "ImageModel": "llava",
    "ImagePrompt": "Look at this photo carefully. ...",
    "TagExistingPrompt": "Look at this photo carefully. Below is a list of existing tags. ..."
  },
  "ImageAnalysis": {
    "FeedSize": 10,
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

### Running

```bash
cd immich-tag-manager
dotnet run
# Open browser: http://localhost:5000
```

### Tests

```bash
cd immich-tag-manager.Tests
dotnet test
```

The test project uses xUnit and covers JSON parsing of Ollama responses and HTTP communication with Immich via a custom `TestHttpMessageHandler`.

---

## immich-seeder

Fetches random photos from Wikimedia Commons and uploads them to Immich in a specified album. Intended for quickly building a test library.

### Configuration

Create `immich-seeder/appsettings.Local.json`:

```json
{
  "Immich": {
    "ApiKey": "<your-api-key>"
  }
}
```

All available settings:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Seeder": {
    "Count": 10,
    "AlbumName": "Test Photos",
    "ConcurrentDownloads": 1
  }
}
```

### Running

```bash
cd immich-seeder
dotnet run
```

---

## Recommended workflow

```
immich-seeder                      →  load test photos into Immich
immich-tag-manager /genereer-tags  →  build a tag hierarchy
immich-tag-manager /analyseer-fotos →  process photos via the browser
  or: ollama2immich                →  process photos via the console
immich-tag-manager /               →  clean up and organise tags
ollama2immich --reset              →  clear everything and start over
```
