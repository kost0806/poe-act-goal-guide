using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Progress;

/// <summary>One goal as displayed.</summary>
/// <param name="Goal">The goal.</param>
/// <param name="IsHere">True when the player is in one of the goal's areas.</param>
public sealed record GoalView(Goal Goal, bool IsHere);

/// <summary>A titled group of goals of one kind.</summary>
/// <param name="Kind">The kind shared by every goal in the section.</param>
/// <param name="Title">Section heading.</param>
/// <param name="Goals">Goals in guide order.</param>
public sealed record SectionView(GoalKind Kind, string Title, IReadOnlyList<GoalView> Goals);

/// <summary>Everything the overlay draws.</summary>
/// <param name="Act">The act being shown.</param>
/// <param name="IsManual">True when the act was picked by hand rather than detected.</param>
/// <param name="CurrentArea">The area the player is in, when known.</param>
/// <param name="Sections">Non-empty sections in <see cref="GoalKind"/> order.</param>
public sealed record GuideView(int Act, bool IsManual, AreaInfo? CurrentArea, IReadOnlyList<SectionView> Sections);
