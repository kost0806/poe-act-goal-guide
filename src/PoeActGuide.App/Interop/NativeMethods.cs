using System.Runtime.InteropServices;

namespace PoeActGuide.App.Interop;

/// <summary>The few Win32 calls the overlay needs.</summary>
internal static partial class NativeMethods
{
    internal const int GwlExStyle = -20;
    internal const int WsExTransparent = 0x00000020;
    internal const int WsExToolWindow = 0x00000080;
    internal const int WsExNoActivate = 0x08000000;
    internal const int WmHotkey = 0x0312;
    internal const uint ModNoRepeat = 0x4000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial nint GetWindowLongPtr64(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLongPtr64(nint hwnd, int index, nint value);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static partial int GetWindowLong32(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static partial int SetWindowLong32(nint hwnd, int index, int value);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(nint hwnd, int id);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint icon);

    /// <summary>Turns extended-style bits on and off, leaving every other bit alone.</summary>
    internal static void UpdateExtendedStyle(nint hwnd, int set, int clear)
    {
        if (nint.Size == 8)
        {
            var current = GetWindowLongPtr64(hwnd, GwlExStyle);
            SetWindowLongPtr64(hwnd, GwlExStyle, (current | set) & ~(nint)clear);
        }
        else
        {
            var current = GetWindowLong32(hwnd, GwlExStyle);
            SetWindowLong32(hwnd, GwlExStyle, (current | set) & ~clear);
        }
    }
}
