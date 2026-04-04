using System.Text.Json.Serialization;

namespace ImmichSeeder.Models;

record WikimediaRandomResponse(WikimediaRandomQuery Query);
record WikimediaRandomQuery(WikimediaRandomFile[] Random);
record WikimediaRandomFile(int Id, string Title);

record WikimediaImageInfoResponse(WikimediaImageInfoQuery Query);
record WikimediaImageInfoQuery(Dictionary<string, WikimediaPage> Pages);
record WikimediaPage([property: JsonPropertyName("imageinfo")] WikimediaImageInfo[]? Imageinfo);
record WikimediaImageInfo(string Url);
