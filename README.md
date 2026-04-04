# ollama2immich

Itereert over alle foto's in een [Immich](https://immich.app/) bibliotheek, stuurt elke thumbnail naar een lokaal [Ollama](https://ollama.com/) vision model, en schrijft de gegenereerde beschrijving en trefwoord-tags terug naar Immich.

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Een draaiende Immich-instantie met een API-key
- Een draaiende Ollama-instantie met een vision model (standaard: `llava`)

## Installatie

```bash
git clone <repo-url>
cd ollama2immich/ollama2immich
```

## Configuratie

Maak een bestand `appsettings.Local.json` aan in de projectmap (naast `appsettings.json`). Dit bestand staat in `.gitignore` en wordt nooit ingecheckt.

```json
{
  "Immich": {
    "ApiKey": "<jouw-api-key>"
  }
}
```

De Immich API-key vind je in de Immich-webinterface onder **Account Settings → API Keys**. De key heeft minimaal de volgende scopes nodig:

| Scope | Waarvoor |
|---|---|
| `asset.read` | Metadata en thumbnails ophalen |
| `asset.view` | Thumbnail-afbeelding downloaden |
| `asset.update` | Beschrijving terugschrijven |
| `tag.read` | Bestaande tags ophalen |
| `tag.create` | Nieuwe tags aanmaken |

### Alle beschikbare instellingen

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llava",
    "Prompt": "Bekijk deze foto aandachtig. Schrijf een natuurlijke beschrijving van 1-2 zinnen in het Nederlands van wat je ziet. Geef daarna maximaal 10 korte trefwoorden in het Nederlands (objecten, personen, plaatsen, sfeer, kleuren)."
  },
  "Processing": {
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

De prompt is volledig aanpasbaar via `Ollama:Prompt`. Het model antwoordt altijd in een vast JSON-formaat dankzij [structured outputs](https://docs.ollama.com/capabilities/structured-outputs) — de prompt hoeft geen opmaakregels te bevatten.

Instellingen kunnen ook als omgevingsvariabele worden meegegeven, met `__` als scheidingsteken:

```bash
Immich__ApiKey=abc123 dotnet run
```

## Uitvoeren

```bash
cd ollama2immich/ollama2immich
dotnet run
```

Foto's die al een beschrijving hebben worden overgeslagen. Alleen assets van het type `IMAGE` worden verwerkt.

## Publiceren

```bash
dotnet publish -c Release
```

Het binaire bestand staat daarna in `bin/Release/net10.0/publish/`.

## Werking

1. Alle bestaande Immich-tags worden eenmalig opgehaald en gecacht.
2. Assets worden gepagineerd opgehaald via `POST /api/search/metadata` met een instelbare concurrentie (`ConcurrentAssets`).
3. Per foto: thumbnail downloaden → via Ollama analyseren met structured output → beschrijving en tags terugschrijven naar Immich.
4. Nieuwe tags worden aangemaakt als ze nog niet bestaan.
