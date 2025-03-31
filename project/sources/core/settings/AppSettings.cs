using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace Core.Settings;

public abstract class AppSettings
{
    public const string VersionCategory = "version";

    private readonly List<ISettingsEntry> settingsEntries = [];

    protected abstract string ConfigFilePath
    {
        get;
    }

    protected Version? AppVersion
    {
        get;
        private set;
    }

    protected Version? PreviousAppVersion
    {
        get;
        private set;
    }

    public void Load(Version appVersion)
    {
        ConfigFile configFile = new();
        Error err = configFile.Load(this.ConfigFilePath);
        switch (err)
        {
            case Error.Ok:
                break;

            case Error.FileNotFound:
                Trace.TraceWarning($"Can't load app_settings.cfg ({err})");
                break;

            default:
                Trace.TraceError($"Can't load app_settings.cfg ({err})");
                break;
        }

        this.AppVersion = appVersion;
        string previousAppVersionString = configFile.GetValue(VersionCategory, "AppVersion", @default: "0.0.0").As<string>();
        this.PreviousAppVersion = new(previousAppVersionString);

        foreach (ISettingsEntry entry in this.settingsEntries)
        {
            entry.Load(configFile);
        }

        if (this.PreviousAppVersion != this.AppVersion)
        {
            Trace.WriteLine($"Migrate settings from v{this.PreviousAppVersion} to v{this.AppVersion}...");
            this.MigrateSettings(this.PreviousAppVersion, this.AppVersion);
            this.Save(this.AppVersion);
        }
    }

    public void Save(Version appVersion)
    {
        ConfigFile configFile = new();

        configFile.SetValue(VersionCategory, "AppVersion", appVersion.ToString());

        foreach (ISettingsEntry entry in this.settingsEntries)
        {
            if (!entry.IsDefault)
            {
                entry.Save(configFile);
            }
        }

        configFile.Save(this.ConfigFilePath);
    }

    protected T RegisterEntry<T>(T entry)
        where T : ISettingsEntry
    {
        Debug.Assert(!this.settingsEntries.Any(settingsEntry => settingsEntry.Path == entry.Path), $"Settings already registered at path {entry.Path}");
        this.settingsEntries.Add(entry);
        return entry;
    }

    protected virtual void MigrateSettings(Version previousVersion, Version currentAppVersion)
    {
    }
}
