using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Help;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.Core.Tests;

public class HelpContentTests
{
    [Fact]
    public void Lists_every_configured_hotkey_in_readable_form()
    {
        var keys = HelpContent.Build(new HotkeySettings())
            .Single(s => s.Title == "단축키")
            .Lines.Select(l => l.Key)
            .ToList();

        Assert.Equal(
            new[]
            {
                "Ctrl+Shift+G", "Ctrl+Shift+→", "Ctrl+Shift+←", "Ctrl+Shift+PgDn",
                "Ctrl+Shift+PgUp", "Ctrl+Shift+C", "Ctrl+Shift+M", "Ctrl+Shift+H",
            },
            keys);
    }

    [Fact]
    public void Follows_changed_and_disabled_hotkeys()
    {
        var settings = new HotkeySettings { NextStep = "Alt+F2", ToggleVisible = string.Empty };

        var lines = HelpContent.Build(settings).Single(s => s.Title == "단축키").Lines;

        Assert.Equal("Alt+F2", lines.Single(l => l.Text.StartsWith("다음 단계", StringComparison.Ordinal)).Key);
        Assert.Equal(HelpContent.DisabledKey, lines.Single(l => l.Text.StartsWith("오버레이 보이기", StringComparison.Ordinal)).Key);
    }

    [Fact]
    public void Explains_every_step_tag()
    {
        var tags = HelpContent.Build(new HotkeySettings())
            .Single(s => s.Title == "꼬리표")
            .Lines.Select(l => l.Tag);

        Assert.Equal(Enum.GetValues<StepKind>().Cast<StepKind?>(), tags);
    }
}
