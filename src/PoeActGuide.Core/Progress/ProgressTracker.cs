using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Progress;

/// <summary>
/// Knows where the player is and which act's goals to show. Not thread-safe: drive it from one thread.
/// </summary>
public sealed class ProgressTracker
{
    private readonly GuideData _data;

    /// <summary>Creates a tracker that starts on act 1 with no known area.</summary>
    /// <param name="data">The guide.</param>
    public ProgressTracker(GuideData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>Raised after anything that changes <see cref="BuildView"/>'s result.</summary>
    public event EventHandler? Changed;

    /// <summary>The last area entered, as logged (may be an area the guide does not know).</summary>
    public string? CurrentAreaId { get; private set; }

    /// <summary>The act of the last known campaign area.</summary>
    public int DetectedAct { get; private set; } = GuideData.FirstAct;

    /// <summary>An act chosen by hand; cleared by the next area change.</summary>
    public int? ManualAct { get; private set; }

    /// <summary>The act whose goals are shown.</summary>
    public int DisplayedAct => ManualAct ?? DetectedAct;

    /// <summary>
    /// True for areas that should not move the act: the Labyrinth entrance is an act 3 area, but it is
    /// visited again from acts 4, 7 and 10.
    /// </summary>
    /// <param name="areaId">Area ID.</param>
    /// <returns>True when entering the area keeps the current act.</returns>
    public static bool IsActNeutral(string areaId) => areaId.StartsWith("Labyrinth_", StringComparison.Ordinal);

    /// <summary>Records an area change from the log.</summary>
    /// <param name="areaId">The area entered.</param>
    public void OnAreaEntered(string areaId)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaId);
        CurrentAreaId = areaId;
        if (_data.Areas.TryGetValue(areaId, out var area) && !IsActNeutral(areaId))
        {
            DetectedAct = Math.Clamp(area.Act, GuideData.FirstAct, GuideData.LastAct);
        }

        ManualAct = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shows the next act (manual override).</summary>
    public void ShowNextAct() => SetManualAct(DisplayedAct + 1);

    /// <summary>Shows the previous act (manual override).</summary>
    public void ShowPreviousAct() => SetManualAct(DisplayedAct - 1);

    /// <summary>Returns to the detected act.</summary>
    public void ClearManualAct()
    {
        if (ManualAct is null)
        {
            return;
        }

        ManualAct = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Builds what the overlay shows.</summary>
    /// <param name="compact">
    /// When true, every main goal is kept but other sections only keep goals that are here or marked key.
    /// </param>
    /// <returns>The view.</returns>
    public GuideView BuildView(bool compact = false)
    {
        var act = _data.GetAct(DisplayedAct);
        var here = CurrentAreaId;
        var sections = new List<SectionView>();

        foreach (var kind in Enum.GetValues<GoalKind>())
        {
            var goals = act.Goals
                .Where(g => g.Kind == kind)
                .Select(g => new GoalView(g, here is not null && g.Areas.Contains(here, StringComparer.Ordinal)))
                .Where(v => !compact || kind == GoalKind.Main || v.IsHere || v.Goal.Key)
                .ToList();
            if (goals.Count > 0)
            {
                sections.Add(new SectionView(kind, TitleOf(kind), goals));
            }
        }

        AreaInfo? area = null;
        if (here is not null)
        {
            _data.Areas.TryGetValue(here, out area);
        }

        return new GuideView(DisplayedAct, ManualAct is not null, area, sections);
    }

    /// <summary>The section heading for a kind.</summary>
    /// <param name="kind">Goal kind.</param>
    /// <returns>Korean heading.</returns>
    public static string TitleOf(GoalKind kind) => kind switch
    {
        GoalKind.Main => "진행",
        GoalKind.Trial => "전직",
        GoalKind.Passive => "패시브 포인트",
        GoalKind.Recipe => "조합법",
        GoalKind.Quest => "유용한 퀘스트",
        _ => kind.ToString(),
    };

    private void SetManualAct(int act)
    {
        var clamped = Math.Clamp(act, GuideData.FirstAct, GuideData.LastAct);
        var next = clamped == DetectedAct ? (int?)null : clamped;
        if (next == ManualAct)
        {
            return;
        }

        ManualAct = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
