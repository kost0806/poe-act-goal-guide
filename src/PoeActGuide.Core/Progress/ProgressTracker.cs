using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Progress;

/// <summary>
/// Follows the player along the route: which act, and which step of it. Not thread-safe: drive it
/// from one thread.
/// </summary>
/// <remarks>
/// <para>
/// Each act keeps its own position. Entering an area moves the position forward to the nearest step
/// within <see cref="Lookahead"/> steps (one step for towns) that lists the area; areas that only appear earlier in the
/// route (going back to town, a waypoint you already used) leave it alone. The route is followed
/// before the area's own act is consulted, so a step such as "go back to act 1 and talk to Bestel"
/// keeps act 2 on screen. Leaving an area counts the consecutive steps done there as passed, once
/// the next area matches the route.
/// </para>
/// <para>
/// When the act changes and nothing ahead matches, the act's route is searched from its start
/// instead: that is a new character, or a replay of an old log.
/// </para>
/// </remarks>
public sealed class ProgressTracker
{
    /// <summary>How many steps ahead an area change may jump.</summary>
    public const int Lookahead = 4;

    /// <summary>Steps shown in <see cref="ViewMode.Normal"/>: one done step, the current one, and the rest ahead.</summary>
    public const int NormalCount = 8;

    /// <summary>Steps shown in <see cref="ViewMode.Compact"/>, starting at the current one.</summary>
    public const int CompactCount = 3;

    /// <summary>The first area of a new character; entering it resets every act's position.</summary>
    public const string NewCharacterAreaId = "1_1_1";

    private readonly GuideData _data;
    private readonly Dictionary<int, int> _positions = new();

    /// <summary>Creates a tracker that starts on act 1, before its first step.</summary>
    /// <param name="data">The guide.</param>
    public ProgressTracker(GuideData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>Raised after anything that changes <see cref="BuildView"/>'s result.</summary>
    public event EventHandler? Changed;

    /// <summary>The last area entered, as logged (may be an area the guide does not know).</summary>
    public string? CurrentAreaId { get; private set; }

    /// <summary>The act the player is following.</summary>
    public int DetectedAct { get; private set; } = GuideData.FirstAct;

    /// <summary>An act chosen by hand; cleared by the next area change.</summary>
    public int? ManualAct { get; private set; }

    /// <summary>The act whose route is shown.</summary>
    public int DisplayedAct => ManualAct ?? DetectedAct;

    /// <summary>
    /// True for areas whose own act should never be switched to: the Labyrinth entrance is an act 3
    /// area, but it is visited again from acts 4, 7 and 10.
    /// </summary>
    /// <param name="areaId">Area ID.</param>
    /// <returns>True when entering the area keeps the current act.</returns>
    public static bool IsActNeutral(string areaId) => areaId.StartsWith("Labyrinth_", StringComparison.Ordinal);

    /// <summary>The 0-based current step of an act, or -1 before its first step.</summary>
    /// <param name="act">Act number.</param>
    /// <returns>The position.</returns>
    public int PositionOf(int act) => _positions.TryGetValue(act, out var p) ? p : -1;

    /// <summary>Records an area change from the log.</summary>
    /// <param name="areaId">The area entered.</param>
    public void OnAreaEntered(string areaId)
    {
        ArgumentException.ThrowIfNullOrEmpty(areaId);
        var previousAreaId = CurrentAreaId;
        CurrentAreaId = areaId;
        ManualAct = null;

        if (areaId == NewCharacterAreaId)
        {
            _positions.Clear();
        }

        if (!TryAdvance(DetectedAct, areaId, previousAreaId)
            && _data.Areas.TryGetValue(areaId, out var area)
            && !IsActNeutral(areaId)
            && area.Act != DetectedAct)
        {
            DetectedAct = Math.Clamp(area.Act, GuideData.FirstAct, GuideData.LastAct);
            if (!TryAdvance(DetectedAct, areaId, previousAreaId: null))
            {
                TryStartOver(DetectedAct, areaId);
            }
        }

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

    /// <summary>Marks the current step done and moves to the next one (manual correction).</summary>
    public void NextStep() => MoveStep(+1);

    /// <summary>Moves back one step (manual correction).</summary>
    public void PreviousStep() => MoveStep(-1);

    /// <summary>Builds what the overlay shows.</summary>
    /// <param name="mode">How much of the route to include.</param>
    /// <returns>The view.</returns>
    public GuideView BuildView(ViewMode mode = ViewMode.Full)
    {
        var act = DisplayedAct;
        var steps = _data.GetAct(act).Steps;
        var position = PositionOf(act);
        var here = CurrentAreaId;

        var views = new List<StepView>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
        {
            var state = i < position ? StepState.Done : i == position ? StepState.Current : StepState.Upcoming;
            var isHere = here is not null && steps[i].Areas.Contains(here, StringComparer.Ordinal);
            views.Add(new StepView(i + 1, steps[i], state, isHere));
        }

        IReadOnlyList<StepView> shown = mode switch
        {
            ViewMode.Compact => views.Skip(Math.Max(position, 0)).Take(CompactCount).ToList(),
            ViewMode.Normal => views.Skip(Math.Max(position - 1, 0)).Take(NormalCount).ToList(),
            _ => views,
        };

        AreaInfo? area = null;
        if (here is not null)
        {
            _data.Areas.TryGetValue(here, out area);
        }

        return new GuideView(act, ManualAct is not null, area, steps.Count, position + 1, shown);
    }

    /// <summary>The tag shown in front of a step.</summary>
    /// <param name="kind">Step kind.</param>
    /// <returns>Korean tag.</returns>
    public static string TagOf(StepKind kind) => kind switch
    {
        StepKind.Main => "진행",
        StepKind.Trial => "전직",
        StepKind.Passive => "패시브",
        StepKind.Recipe => "작업대",
        StepKind.Quest => "퀘스트",
        _ => kind.ToString(),
    };

    private bool TryAdvance(int act, string areaId, string? previousAreaId)
    {
        var steps = _data.GetAct(act).Steps;
        var position = PositionOf(act);

        // Leaving an area finishes the run of consecutive steps done there (e.g. two kills in the same
        // forest). This only takes effect if the new area then matches the route, so stepping out to
        // town and back in does not tick anything off.
        if (previousAreaId is not null && position >= 0)
        {
            while (position + 1 < steps.Count
                && steps[position].Areas.Contains(previousAreaId, StringComparer.Ordinal)
                && steps[position + 1].Areas.Contains(previousAreaId, StringComparer.Ordinal)
                && !steps[position].Areas.Contains(areaId, StringComparer.Ordinal))
            {
                position++;
            }
        }

        var from = Math.Max(position, 0);
        // Towns are revisited constantly (logging out, handing in), so they may only advance to the
        // very next step; otherwise every trip to town would skip to the next hand-in on the route.
        var isTown = _data.Areas.TryGetValue(areaId, out var area) && area.Town;
        var to = Math.Min(steps.Count - 1, position + (isTown ? 1 : Lookahead));
        for (var i = from; i <= to; i++)
        {
            if (steps[i].Areas.Contains(areaId, StringComparer.Ordinal))
            {
                _positions[act] = i;
                return true;
            }
        }

        return false;
    }

    private void TryStartOver(int act, string areaId)
    {
        var steps = _data.GetAct(act).Steps;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Areas.Contains(areaId, StringComparer.Ordinal))
            {
                _positions[act] = i;
                return;
            }
        }
    }

    private void MoveStep(int delta)
    {
        var act = DisplayedAct;
        var count = _data.GetAct(act).Steps.Count;
        var next = Math.Clamp(PositionOf(act) + delta, -1, count - 1);
        if (next == PositionOf(act))
        {
            return;
        }

        _positions[act] = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }

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
