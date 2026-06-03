using System.IO;
using System.Text.Json;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ConfigDirectory { get; }
    public string ConfigPath { get; }

    public ConfigStore()
    {
        ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GamerGuardian");
        ConfigPath = Path.Combine(ConfigDirectory, "config.json");
    }

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            // Collapse duplicate per-display entries left behind by older,
            // key-unstable versions so saved settings stop "resetting".
            DisplayPreferenceResolver.DedupeDisplays(config);
            return config;
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
