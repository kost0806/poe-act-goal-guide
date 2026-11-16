using System.Text.Json;

namespace PoeActGuide.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/>.</summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Creates a store over one file.</summary>
    /// <param name="path">The settings file.</param>
    public SettingsStore(string path)
    {
        Path = path;
    }

    /// <summary>The settings file.</summary>
    public string Path { get; }

    /// <summary>The default location: %APPDATA%\PoeActGuide\settings.json.</summary>
    /// <returns>The path.</returns>
    public static string DefaultPath() =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PoeActGuide", "settings.json");

    /// <summary>
    /// Loads the settings. A missing file yields defaults (and is created); an unreadable one is moved
    /// aside to <c>settings.json.bak</c> and defaults are used.
    /// </summary>
    /// <returns>The settings, always valid.</returns>
    public AppSettings Load()
    {
        if (!File.Exists(Path))
        {
            var defaults = new AppSettings();
            TrySave(defaults);
            return defaults;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path), Options);
            return (loaded ?? new AppSettings()).Normalize();
        }
        catch (JsonException)
        {
            TryMoveAside();
            return new AppSettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Writes the settings, replacing the file atomically.</summary>
    /// <param name="settings">What to save.</param>
    /// <returns>True when the file was written.</returns>
    public bool TrySave(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var temp = Path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
            File.Move(temp, Path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void TryMoveAside()
    {
        try
        {
            File.Move(Path, Path + ".bak", overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Leave it; the next save overwrites it.
        }
    }
}
