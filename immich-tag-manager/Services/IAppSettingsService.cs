using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public interface IAppSettingsService
{
    AppSettings GetSettings();
    Task SaveSettingsAsync(AppSettings settings);
}
