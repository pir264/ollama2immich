using ImmichTagManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var cfg = builder.Configuration;
var immichBase  = cfg["Immich:BaseUrl"]?.TrimEnd('/') + "/";
var immichKey   = cfg["Immich:ApiKey"] ?? string.Empty;
var ollamaBase  = cfg["Ollama:BaseUrl"]?.TrimEnd('/') + "/";
var ollamaModel = cfg["Ollama:Model"] ?? "gemma4";
var ollamaPrompt = cfg["Ollama:TagPrompt"] ?? string.Empty;

builder.Services.AddHttpClient<IImmichTagService, ImmichTagService>(client =>
{
    client.BaseAddress = new Uri(immichBase!);
    client.DefaultRequestHeaders.Add("x-api-key", immichKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IOllamaTagService>(sp =>
{
    var http = new HttpClient
    {
        BaseAddress = new Uri(ollamaBase!),
        Timeout = TimeSpan.FromMinutes(10)
    };
    var logger = sp.GetRequiredService<ILogger<OllamaTagService>>();
    return new OllamaTagService(http, ollamaModel, ollamaPrompt, logger);
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(immichKey))
{
    app.Logger.LogCritical("Immich:ApiKey is niet ingesteld.");
    Environment.Exit(1);
}
if (string.IsNullOrWhiteSpace(ollamaPrompt))
{
    app.Logger.LogCritical("Ollama:TagPrompt is niet ingesteld.");
    Environment.Exit(1);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<ImmichTagManager.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
