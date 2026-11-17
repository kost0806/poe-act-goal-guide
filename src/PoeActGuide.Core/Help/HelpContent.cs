using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Progress;
using PoeActGuide.Core.Settings;

namespace PoeActGuide.Core.Help;

/// <summary>One line of the help screen.</summary>
/// <param name="Text">The explanation.</param>
/// <param name="Key">A hotkey shown in front of the text, if any.</param>
/// <param name="Tag">A step tag shown in front of the text (in its colour), if any.</param>
public sealed record HelpLine(string Text, string? Key = null, StepKind? Tag = null);

/// <summary>A titled group of help lines.</summary>
/// <param name="Title">Section heading.</param>
/// <param name="Lines">The lines.</param>
public sealed record HelpSection(string Title, IReadOnlyList<HelpLine> Lines);

/// <summary>Builds the in-overlay help, using the hotkeys actually configured.</summary>
public static class HelpContent
{
    /// <summary>Shown in place of a hotkey that is turned off (empty in the settings).</summary>
    public const string DisabledKey = "(끔)";

    /// <summary>Builds the help screen.</summary>
    /// <param name="keys">The configured hotkeys.</param>
    /// <returns>Help sections in display order.</returns>
    public static IReadOnlyList<HelpSection> Build(HotkeySettings keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return new[]
        {
            new HelpSection("단축키", new[]
            {
                new HelpLine("오버레이 보이기 / 숨기기", Key(keys.ToggleVisible)),
                new HelpLine("다음 단계 (자동 진행이 어긋났을 때)", Key(keys.NextStep)),
                new HelpLine("이전 단계", Key(keys.PreviousStep)),
                new HelpLine("다음 장 미리 보기 (다음 지역 이동 시 자동으로 돌아옴)", Key(keys.NextAct)),
                new HelpLine("이전 장 미리 보기", Key(keys.PreviousAct)),
                new HelpLine("보기 방식: 기본 → 간단히 → 액트 전체", Key(keys.CycleViewMode)),
                new HelpLine("위치 이동 모드 (켜는 동안 마우스로 끌어 옮김)", Key(keys.ToggleMoveMode)),
                new HelpLine("도움말 열기 / 닫기", Key(keys.ToggleHelp)),
            }),
            new HelpSection("꼬리표", new[]
            {
                new HelpLine("장의 큰 줄기 — 보스 처치, 다음 지역으로", Tag: StepKind.Main),
                new HelpLine("미궁 도전(전직 시험)과 미궁", Tag: StepKind.Trial),
                new HelpLine("패시브 스킬 포인트를 주는 퀘스트", Tag: StepKind.Passive),
                new HelpLine("작업대 옵션이 잠금 해제되는 곳", Tag: StepKind.Recipe),
                new HelpLine("진행을 편하게 해 주는 퀘스트", Tag: StepKind.Quest),
            }),
            new HelpSection("보는 법", new[]
            {
                new HelpLine("노란 줄은 지금 할 단계, 회색은 지난 단계, ★ 는 특히 중요한 단계"),
                new HelpLine("헤더: 장 · 진행 단계 수 · 지금 있는 지역과 레벨"),
                new HelpLine("기본: 직전 1개 + 현재 + 앞으로 6개"),
                new HelpLine("간단히: 직전 1개 + 현재 + 다음 1개"),
                new HelpLine("액트 전체: 그 장의 모든 단계"),
            }),
            new HelpSection("진행 방식", new[]
            {
                new HelpLine("게임 로그를 읽어 지역에 들어갈 때마다 루트의 다음 단계로 넘어갑니다"),
                new HelpLine("다음 장의 첫 지역에 들어서면 장이 바뀝니다"),
                new HelpLine("황혼의 해안에 들어가면 새 캐릭터로 보고 처음부터 시작합니다"),
            }),
            new HelpSection("그 밖에", new[]
            {
                new HelpLine("게임은 창 모드 전체 화면(테두리 없는 창)으로 실행하세요"),
                new HelpLine("오버레이는 클릭이 게임으로 통과합니다. 설정은 작업 표시줄 트레이 아이콘을 우클릭하세요"),
                new HelpLine("로그 파일을 못 찾으면 트레이 메뉴에서 Client.txt 를 직접 지정하세요"),
            }),
        };
    }

    private static string Key(string? gesture) =>
        HotkeyGesture.TryParse(gesture, out var parsed) && parsed is not null
            ? Pretty(parsed)
            : DisabledKey;

    private static string Pretty(HotkeyGesture gesture) => gesture.ToString()
        .Replace("PageUp", "PgUp", StringComparison.Ordinal)
        .Replace("PageDown", "PgDn", StringComparison.Ordinal)
        .Replace("+Right", "+→", StringComparison.Ordinal)
        .Replace("+Left", "+←", StringComparison.Ordinal)
        .Replace("+Up", "+↑", StringComparison.Ordinal)
        .Replace("+Down", "+↓", StringComparison.Ordinal);
}
