using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Progress;

/// <summary>How much of the act's route the overlay shows.</summary>
public enum ViewMode
{
    /// <summary>The previous step, the current one and the next several (with notes).</summary>
    Normal,

    /// <summary>At most one step before the current one, the current step, and at most one after.</summary>
    Compact,

    /// <summary>The whole act.</summary>
    Full,
}

/// <summary>Where a step stands relative to the player's progress.</summary>
public enum StepState
{
    /// <summary>Already passed on the route.</summary>
    Done,

    /// <summary>The step the player is on.</summary>
    Current,

    /// <summary>Still ahead.</summary>
    Upcoming,
}

/// <summary>One step as displayed.</summary>
/// <param name="Number">1-based position in the act's route.</param>
/// <param name="Step">The step.</param>
/// <param name="State">Done, current or upcoming.</param>
/// <param name="IsHere">True when the player is in one of the step's areas.</param>
public sealed record StepView(int Number, RouteStep Step, StepState State, bool IsHere);

/// <summary>Everything the overlay draws.</summary>
/// <param name="Act">The act being shown.</param>
/// <param name="IsManual">True when the act was picked by hand rather than detected.</param>
/// <param name="CurrentArea">The area the player is in, when known.</param>
/// <param name="TotalSteps">Number of steps in the act's route.</param>
/// <param name="CurrentNumber">1-based number of the current step; 0 before the route starts.</param>
/// <param name="Steps">The steps to draw, in route order (a window around the current step, or all of them).</param>
public sealed record GuideView(
    int Act,
    bool IsManual,
    AreaInfo? CurrentArea,
    int TotalSteps,
    int CurrentNumber,
    IReadOnlyList<StepView> Steps);
