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

De Immich API-key vind je in de Immich-webinterface onder **Account Settings → API Keys**.

Alle beschikbare instellingen (met hun standaardwaarden):

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
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
2. Assets worden gepagineerd opgehaald en verwerkt met een instelbare concurrentie (`ConcurrentAssets`).
3. Per foto: thumbnail downloaden → naar Ollama sturen → beschrijving en tags terugschrijven naar Immich.
4. Nieuwe tags worden aangemaakt als ze nog niet bestaan.
