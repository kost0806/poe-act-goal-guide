using PoeActGuide.Core.Progress;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.Core.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("poe-act-guide-settings-").FullName;

    private string SettingsPath => Path.Combine(_dir, "sub", "settings.json");

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    [Theory]
    [InlineData("Ctrl+Shift+G", HotkeyModifiers.Control | HotkeyModifiers.Shift, "G")]
    [InlineData("ctrl + alt + PageDown", HotkeyModifiers.Control | HotkeyModifiers.Alt, "PageDown")]
    [InlineData("F9", HotkeyModifiers.None, "F9")]
    [InlineData("Win+Control+F1", HotkeyModifiers.Win | HotkeyModifiers.Control, "F1")]
    public void Parses_hotkeys(string text, HotkeyModifiers modifiers, string key)
    {
        Assert.True(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Equal(new HotkeyGesture(modifiers, key), gesture);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+G+H")]
    [InlineData("Ctrl+Ctrl+G")]
    [InlineData("Ctrl++G")]
    public void Rejects_invalid_hotkeys(string? text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Null(gesture);
    }

    [Fact]
    public void Hotkey_round_trips_through_text()
    {
        Assert.True(HotkeyGesture.TryParse("shift+ctrl+m", out var gesture));
        Assert.Equal("Ctrl+Shift+m", gesture!.ToString());
    }

    [Fact]
    public void Default_hotkeys_all_parse()
    {
        var keys = new HotkeySettings();

        foreach (var text in new[] { keys.ToggleVisible, keys.PreviousAct, keys.NextAct, keys.NextStep, keys.PreviousStep, keys.CycleViewMode, keys.ToggleMoveMode })
        {
            Assert.True(HotkeyGesture.TryParse(text, out _), text);
        }
    }

    [Fact]
    public void Missing_file_yields_defaults_and_creates_the_file()
    {
        var store = new SettingsStore(SettingsPath);

        var settings = store.Load();

        Assert.Equal(400, settings.Width);
        Assert.True(File.Exists(SettingsPath));
    }

    [Fact]
    public void Saved_settings_load_back()
    {
        var store = new SettingsStore(SettingsPath);
        var settings = new AppSettings { Left = 1234, Top = 56, ViewMode = ViewMode.Compact, ClientLogPath = @"C:\x\Client.txt" };
        settings.Hotkeys.NextAct = "Alt+N";

        Assert.True(store.TrySave(settings));
        var loaded = store.Load();

        Assert.Equal(1234, loaded.Left);
        Assert.Equal(56, loaded.Top);
        Assert.Equal(ViewMode.Compact, loaded.ViewMode);
        Assert.Equal(@"C:\x\Client.txt", loaded.ClientLogPath);
        Assert.Equal("Alt+N", loaded.Hotkeys.NextAct);
    }

    [Fact]
    public void Corrupt_file_is_moved_aside_and_defaults_are_used()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ not json");

        var settings = new SettingsStore(SettingsPath).Load();

        Assert.Equal(new AppSettings().Width, settings.Width);
        Assert.Equal("{ not json", File.ReadAllText(SettingsPath + ".bak"));
    }

    [Fact]
    public void Out_of_range_values_are_clamped()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ \"width\": 5, \"backgroundOpacity\": 3, \"fontSize\": 100, \"hotkeys\": null }");

        var settings = new SettingsStore(SettingsPath).Load();

        Assert.Equal(260, settings.Width);
        Assert.Equal(1, settings.BackgroundOpacity);
        Assert.Equal(28, settings.FontSize);
        Assert.NotNull(settings.Hotkeys);
    }

    [Fact]
    public void View_mode_is_stored_by_name()
    {
        var store = new SettingsStore(SettingsPath);
        store.TrySave(new AppSettings { ViewMode = ViewMode.Full });

        Assert.Contains("\"Full\"", File.ReadAllText(SettingsPath), StringComparison.Ordinal);
        Assert.Equal(ViewMode.Full, store.Load().ViewMode);
    }
}
