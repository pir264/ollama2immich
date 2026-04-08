using ImmichTagManager.Models;
using ImmichTagManager.Services;

namespace ImmichTagManager.Tests;

internal class FakeAppSettingsService(AppSettings settings) : IAppSettingsService
{
    public AppSettings GetSettings() => settings.Clone();
    public Task SaveSettingsAsync(AppSettings s) => Task.CompletedTask;
}
