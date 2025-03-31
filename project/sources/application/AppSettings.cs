using Core.Services;
using Core.Settings;

namespace Application;

public class AppSettings : Core.Settings.AppSettings, ISingleton<App>
{
    public readonly SettingsEntry<string[]> SetlistPaths;

    public AppSettings()
    {
        this.SetlistPaths = this.RegisterEntry(new SettingsEntry<string[]>(
            "main",
            "setlist_paths",
            [],
            this.OnSetlistPathsChange
        ));
    }

    protected override string ConfigFilePath => "user://app_settings.cfg";

    public void Bind()
    {
        this.Load(App.Version);
    }

    private void OnSetlistPathsChange(string[] setlists)
    {
    }
}
