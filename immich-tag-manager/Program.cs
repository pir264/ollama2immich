using ImmichTagManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var cfg = builder.Configuration;
var immichBase  = cfg["Immich:BaseUrl"]?.TrimEnd('/') + "/";
var immichKey   = cfg["Immich:ApiKey"] ?? string.Empty;
var ollamaBase  = cfg["Ollama:BaseUrl"]?.TrimEnd('/') + "/";
var ollamaModel = cfg["Ollama:Model"] ?? "gemma4";
var ollamaPrompt = cfg["Ollama:TagPrompt"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(immichKey))
{
    Console.Error.WriteLine("ERROR: Immich:ApiKey is niet ingesteld.");
    Environment.Exit(1);
}
if (string.IsNullOrWhiteSpace(ollamaPrompt))
{
    Console.Error.WriteLine("ERROR: Ollama:TagPrompt is niet ingesteld.");
    Environment.Exit(1);
}

builder.Services.AddHttpClient<IImmichTagService, ImmichTagService>(client =>
{
    client.BaseAddress = new Uri(immichBase!);
    client.DefaultRequestHeaders.Add("x-api-key", immichKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IOllamaTagService>(_ =>
{
    var http = new HttpClient
    {
        BaseAddress = new Uri(ollamaBase!),
        Timeout = TimeSpan.FromMinutes(10)
    };
    return new OllamaTagService(http, ollamaModel, ollamaPrompt);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<ImmichTagManager.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
