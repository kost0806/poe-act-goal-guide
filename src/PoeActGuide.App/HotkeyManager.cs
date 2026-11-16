using System.Windows.Input;
using System.Windows.Interop;
using PoeActGuide.App.Interop;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.App;

/// <summary>System-wide hotkeys, delivered on the UI thread through the overlay's window procedure.</summary>
internal sealed class HotkeyManager : IDisposable
{
    private readonly nint _hwnd;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 1;

    public HotkeyManager(nint hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("The overlay has no window handle yet.");
        _source.AddHook(WndProc);
    }

    /// <summary>Registers a hotkey.</summary>
    /// <param name="text">Gesture text from the settings; empty disables the hotkey.</param>
    /// <param name="action">What to run.</param>
    /// <returns>Null on success or when disabled; otherwise why it failed (for the user).</returns>
    public string? Register(string text, Action action)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!HotkeyGesture.TryParse(text, out var gesture) || gesture is null
            || !Enum.TryParse<Key>(gesture.Key, ignoreCase: true, out var key) || key == Key.None)
        {
            return $"'{text}': 단축키 형식을 알 수 없습니다";
        }

        var id = _nextId++;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!NativeMethods.RegisterHotKey(_hwnd, id, (uint)gesture.Modifiers | NativeMethods.ModNoRepeat, vk))
        {
            return $"'{text}': 다른 프로그램이 이미 사용 중입니다";
        }

        _actions[id] = action;
        return null;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys)
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }

        _actions.Clear();
        _source.RemoveHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey && _actions.TryGetValue((int)wParam, out var action))
        {
            handled = true;
            action();
        }

        return 0;
    }
}
