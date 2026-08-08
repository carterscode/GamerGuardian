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
        // Paths come from AppIdentity so a beta build's state lands under its own
        // root -- and so ChangeLogger, which writes into the same folder, can never
        // disagree about where that folder is.
        ConfigDirectory = AppIdentity.ConfigDirectory;
        ConfigPath = AppIdentity.ConfigFile;
    }

    /// <summary>Points the store at an explicit directory. Used by tests so
    /// <see cref="Load"/> and <see cref="Save"/> can be exercised against a temp
    /// folder instead of the real %APPDATA% root.</summary>
    public ConfigStore(string configDirectory)
    {
        ConfigDirectory = configDirectory;
        ConfigPath = Path.Combine(configDirectory, "config.json");
    }

    /// <summary>
    /// First-launch seed for a beta build: copies <c>config.json</c> out of the
    /// stable install's folder so a tester starts from their real settings instead
    /// of defaults.
    ///
    /// <para>Strictly one-way. It reads from <paramref name="sourceDirectory"/> and
    /// only ever writes under <paramref name="targetDirectory"/>, so a beta build
    /// can never modify the stable install's config. It no-ops when the two
    /// directories are the same (a stable build), when the target already exists
    /// (not a first launch), or when there is nothing to copy. Failure is silent
    /// and simply leaves the caller starting from defaults, which is the current
    /// behavior anyway.</para>
    ///
    /// <para>Returns true only when a file was actually copied.</para>
    /// </summary>
    public static bool SeedConfigFrom(string sourceDirectory, string targetDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(targetDirectory))
                return false;
            if (string.Equals(sourceDirectory, targetDirectory, StringComparison.OrdinalIgnoreCase))
                return false;
            // Only on a genuine first launch: an existing target root means this
            // build already has state of its own, which must not be overwritten.
            if (Directory.Exists(targetDirectory)) return false;

            var source = Path.Combine(sourceDirectory, "config.json");
            if (!File.Exists(source)) return false;

            Directory.CreateDirectory(targetDirectory);
            File.Copy(source, Path.Combine(targetDirectory, "config.json"), overwrite: false);
            return true;
        }
        catch
        {
            return false;
        }
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
