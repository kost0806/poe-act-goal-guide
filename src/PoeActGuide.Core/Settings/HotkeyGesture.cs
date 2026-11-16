namespace PoeActGuide.Core.Settings;

/// <summary>Modifier keys; the values are the Win32 <c>MOD_*</c> flags used by <c>RegisterHotKey</c>.</summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Alt.</summary>
    Alt = 0x1,

    /// <summary>Ctrl.</summary>
    Control = 0x2,

    /// <summary>Shift.</summary>
    Shift = 0x4,

    /// <summary>The Windows key.</summary>
    Win = 0x8,
}

/// <summary>A parsed hotkey such as "Ctrl+Shift+PageUp".</summary>
/// <param name="Modifiers">Modifier keys.</param>
/// <param name="Key">The main key's name (a WPF <c>Key</c> name such as <c>G</c>, <c>PageUp</c>, <c>F5</c>).</param>
public sealed record HotkeyGesture(HotkeyModifiers Modifiers, string Key)
{
    /// <summary>Parses "Mod+Mod+Key". Modifier names are case-insensitive.</summary>
    /// <param name="text">The gesture text; empty means "no hotkey".</param>
    /// <param name="gesture">The gesture when parsing succeeded.</param>
    /// <returns>False for empty text, a missing key, two keys, or a repeated modifier.</returns>
    public static bool TryParse(string? text, out HotkeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var modifiers = HotkeyModifiers.None;
        string? key = null;
        foreach (var raw in text.Split('+'))
        {
            var part = raw.Trim();
            var modifier = part.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => HotkeyModifiers.Control,
                "SHIFT" => HotkeyModifiers.Shift,
                "ALT" => HotkeyModifiers.Alt,
                "WIN" or "WINDOWS" => HotkeyModifiers.Win,
                _ => HotkeyModifiers.None,
            };

            if (modifier != HotkeyModifiers.None)
            {
                if ((modifiers & modifier) != 0)
                {
                    return false;
                }

                modifiers |= modifier;
            }
            else if (part.Length == 0 || key is not null)
            {
                return false;
            }
            else
            {
                key = part;
            }
        }

        if (key is null)
        {
            return false;
        }

        gesture = new HotkeyGesture(modifiers, key);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Win))
        {
            parts.Add("Win");
        }

        parts.Add(Key);
        return string.Join('+', parts);
    }
}
