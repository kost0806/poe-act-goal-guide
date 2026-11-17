using System.Text.Json.Serialization;
using PoeActGuide.Core.Progress;

namespace PoeActGuide.Core.Settings;

/// <summary>User settings, stored as JSON in %APPDATA%\PoeActGuide\settings.json.</summary>
public sealed class AppSettings
{
    /// <summary>Path of Client.txt / KakaoClient.txt; null or empty to detect automatically.</summary>
    public string? ClientLogPath { get; set; }

    /// <summary>Overlay left edge in device-independent pixels.</summary>
    public double Left { get; set; } = 20;

    /// <summary>Overlay top edge in device-independent pixels.</summary>
    public double Top { get; set; } = 160;

    /// <summary>Overlay width in device-independent pixels; the height follows the content.</summary>
    public double Width { get; set; } = 400;

    /// <summary>Opacity of the overlay background, 0.2–1. Text stays fully opaque.</summary>
    public double BackgroundOpacity { get; set; } = 0.78;

    /// <summary>Base font size.</summary>
    public double FontSize { get; set; } = 13;

    /// <summary>How much of the route to show: Normal (기본), Compact (간단히) or Full (전체).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ViewMode>))]
    public ViewMode ViewMode { get; set; } = ViewMode.Normal;

    /// <summary>Open the help screen at the next start; turned off once it has been shown.</summary>
    public bool ShowHelpOnStart { get; set; } = true;

    /// <summary>Global hotkeys.</summary>
    public HotkeySettings Hotkeys { get; set; } = new();

    /// <summary>Clamps every value into its valid range and fills in missing parts.</summary>
    /// <returns>This instance.</returns>
    public AppSettings Normalize()
    {
        Width = Clamp(Width, 260, 1000, 400);
        BackgroundOpacity = Clamp(BackgroundOpacity, 0.2, 1, 0.78);
        FontSize = Clamp(FontSize, 9, 28, 13);
        Left = double.IsFinite(Left) ? Left : 20;
        Top = double.IsFinite(Top) ? Top : 160;
        Hotkeys ??= new HotkeySettings();
        if (!Enum.IsDefined(ViewMode))
        {
            ViewMode = ViewMode.Normal;
        }

        return this;
    }

    private static double Clamp(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}

/// <summary>Global hotkeys as text, e.g. "Ctrl+Shift+G". Empty disables a hotkey.</summary>
public sealed class HotkeySettings
{
    /// <summary>Show or hide the overlay.</summary>
    public string ToggleVisible { get; set; } = "Ctrl+Shift+G";

    /// <summary>Show the previous act.</summary>
    public string PreviousAct { get; set; } = "Ctrl+Shift+PageUp";

    /// <summary>Show the next act.</summary>
    public string NextAct { get; set; } = "Ctrl+Shift+PageDown";

    /// <summary>Mark the current step done and move to the next one.</summary>
    public string NextStep { get; set; } = "Ctrl+Shift+Right";

    /// <summary>Move back one step.</summary>
    public string PreviousStep { get; set; } = "Ctrl+Shift+Left";

    /// <summary>Cycle the view mode: 기본 → 간단히 → 전체.</summary>
    public string CycleViewMode { get; set; } = "Ctrl+Shift+C";

    /// <summary>Enter or leave move mode (drag the overlay with the mouse).</summary>
    public string ToggleMoveMode { get; set; } = "Ctrl+Shift+M";

    /// <summary>Show or hide the help screen.</summary>
    public string ToggleHelp { get; set; } = "Ctrl+Shift+H";
}
