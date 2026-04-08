using ImmichTagManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

builder.Services.AddHttpClient<IImmichTagService, ImmichTagService>(client =>
    client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<IImmichAssetService, ImmichAssetService>(client =>
    client.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddSingleton<IOllamaTagService>(sp =>
{
    var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    return new OllamaTagService(http, sp.GetRequiredService<IAppSettingsService>(),
        sp.GetRequiredService<ILogger<OllamaTagService>>());
});

builder.Services.AddSingleton<IOllamaTagGeneratorService>(sp =>
{
    var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    return new OllamaTagGeneratorService(http, sp.GetRequiredService<IAppSettingsService>(),
        sp.GetRequiredService<ILogger<OllamaTagGeneratorService>>());
});

builder.Services.AddSingleton<IOllamaImageService>(sp =>
{
    var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    return new OllamaImageService(http, sp.GetRequiredService<IAppSettingsService>(),
        sp.GetRequiredService<ILogger<OllamaImageService>>());
});

builder.Services.AddHttpClient<IOllamaModelsService, OllamaModelsService>(client =>
    client.Timeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

var startupSettings = app.Services.GetRequiredService<IAppSettingsService>().GetSettings();
if (string.IsNullOrWhiteSpace(startupSettings.ImmichApiKey))
    app.Logger.LogWarning("Immich ApiKey is niet ingesteld. Stel het in via /instellingen.");

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<ImmichTagManager.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
