# ollama2immich

Een verzameling .NET 10 tools om een [Immich](https://immich.app/) fotobibliotheek automatisch te verrijken en te organiseren met behulp van een lokaal [Ollama](https://ollama.com/) taalmodel.

## Projecten

| Project | Type | Doel |
|---|---|---|
| [`ollama2immich`](#ollama2immich-1) | Console | Genereert beschrijvingen en tags voor elke foto via een vision model |
| [`immich-tag-manager`](#immich-tag-manager-1) | Blazor Server (web) | Organiseert tags: hernoemingen, samenvoegingen en hiërarchie via een web-UI |
| [`immich-seeder`](#immich-seeder-1) | Console | Vult Immich met testfoto's van Wikimedia |

Alle tools draaien volledig lokaal — er wordt geen data naar externe diensten gestuurd.

---

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Een draaiende [Immich](https://immich.app/) instantie
- Een draaiende [Ollama](https://ollama.com/) instantie

---

## ollama2immich

Itereert over alle foto's in de Immich bibliotheek, stuurt elke thumbnail naar een Ollama vision model en schrijft de gegenereerde beschrijving en trefwoord-tags terug naar Immich. Foto's die al een beschrijving hebben worden overgeslagen.

### Werking

1. Alle bestaande Immich-tags worden eenmalig opgehaald en gecacht.
2. Assets worden gepagineerd opgehaald; niet-afbeeldingen en al beschreven foto's worden overgeslagen.
3. Per foto: thumbnail ophalen → via Ollama analyseren (structured output) → beschrijving en tags terugschrijven.
4. Nieuwe tags worden aangemaakt als ze nog niet bestaan.
5. Met `--reset` worden alle beschrijvingen en tags gewist, zodat de verwerking opnieuw kan starten.

### Configuratie

Maak `ollama2immich/appsettings.Local.json` aan (staat in `.gitignore`):

```json
{
  "Immich": {
    "ApiKey": "<jouw-api-key>"
  }
}
```

Alle beschikbare instellingen:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "gemma4",
    "Prompt": "Bekijk deze foto aandachtig. ..."
  },
  "Processing": {
    "ConcurrentAssets": 2,
    "PageSize": 50
  }
}
```

De Immich API-key heeft minimaal deze scopes nodig:

| Scope | Waarvoor |
|---|---|
| `asset.read` | Metadata en thumbnails ophalen |
| `asset.view` | Thumbnail downloaden |
| `asset.update` | Beschrijving terugschrijven |
| `tag.read` | Bestaande tags ophalen |
| `tag.create` | Nieuwe tags aanmaken |

### Uitvoeren

```bash
cd ollama2immich

# Foto's verwerken
dotnet run

# Alle beschrijvingen en tags wissen (handig voor testen)
dotnet run -- --reset
```

---

## immich-tag-manager

Een Blazor Server web-applicatie die de tag-collectie in Immich opschoont. Na het automatisch taggen met `ollama2immich` ontstaat er een grote, vlakke lijst tags — met duplicaten, meervoudsvormen en gemengde talen. Deze tool organiseert dat via drie stappen.

De app heeft twee functies, bereikbaar via de navigatiebalk:

### Tags beheren (`/`)

1. **Analyseren** — de app haalt alle tags op uit Immich en stuurt ze naar Ollama.
2. **Voorstellen** — Ollama stelt drie soorten wijzigingen voor:
   - **Hernoemingen** — tags naar enkelvoud en lowercase (`Bomen` → `boom`)
   - **Samenvoegingen** — synoniemen en duplicaten samenvoegen (`treinen`, `trains` → `trein`); alle foto's van de te verwijderen tag worden overgezet naar de te bewaren tag
   - **Hiërarchie** — abstracte parent-tags bedenken en toewijzen (`boom`, `waterval` → parent `natuur`); nieuwe parent-tags worden aangemaakt indien nodig
3. **Goedkeuren** — elke suggestie verschijnt als checkbox; de gebruiker vinkelt aan wat toegepast moet worden.
4. **Toepassen** — na bevestiging worden de wijzigingen in volgorde doorgevoerd naar Immich, met een live progress-log in de browser.

### Hiërarchie genereren (`/genereer-tags`)

Laat Ollama — zonder foto's te analyseren — een algemeen bruikbare taghiërarchie bedenken voor een fotobibliotheek. Configureerbaar: maximaal aantal tags (standaard 100) en hiërarchiediepte (standaard 3). Alle tags zijn in het Nederlands, enkelvoud en kleine letters. Voorbeeldpaden: `locatie → land → italië`, `natuur → kleur → rood`.

1. **Genereren** — Ollama redeneert over categorieën zoals locatie, natuur, mensen, gebouwen, voedsel, vervoer, seizoenen, kleuren en activiteiten.
2. **Goedkeuren** — elk tagpad verschijnt als checkbox; selecteer wat relevant is.
3. **Toepassen** — de geselecteerde paden worden als geneste tags in Immich opgeslagen (met parent-child relaties). Bestaande tags worden hergebruikt.

### Configuratie

Maak `immich-tag-manager/appsettings.Local.json` aan:

```json
{
  "Immich": {
    "ApiKey": "<jouw-api-key>"
  }
}
```

Alle beschikbare instellingen:

```json
{
  "Immich": {
    "BaseUrl": "http://localhost:2283",
    "ApiKey": ""
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "gemma4",
    "TagPrompt": "Je krijgt een lijst met Immich fototags. ...",
    "MaxGeneratedTags": 100,
    "TagGeneratorDepth": 3,
    "TagGeneratorPrompt": "Je bent een expert in het organiseren van fotobibliotheken. ..."
  }
}
```

### Uitvoeren

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

Het testproject gebruikt xUnit en NSubstitute en dekt de JSON-parsing van Ollama-responses en de HTTP-communicatie met Immich.

---

## immich-seeder

Haalt willekeurige foto's op van Wikimedia Commons en uploadt ze naar Immich in een opgegeven album. Bedoeld om snel een testbibliotheek op te bouwen.

### Configuratie

Maak `immich-seeder/appsettings.Local.json` aan:

```json
{
  "Immich": {
    "ApiKey": "<jouw-api-key>"
  }
}
```

Alle beschikbare instellingen:

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

### Uitvoeren

```bash
cd immich-seeder
dotnet run
```

---

## Aanbevolen workflow

```
immich-seeder        →  testfoto's in Immich laden
ollama2immich        →  beschrijvingen en tags genereren per foto
immich-tag-manager   →  tags organiseren in een hiërarchie
ollama2immich --reset →  alles wissen en opnieuw beginnen
```
