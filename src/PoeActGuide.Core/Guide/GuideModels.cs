namespace PoeActGuide.Core.Guide;

/// <summary>What sort of goal a guide entry is. Also the order sections appear in.</summary>
public enum GoalKind
{
    /// <summary>The act's main progression — "kill X in Y".</summary>
    Main,

    /// <summary>Trials of Ascendancy and the Labyrinth.</summary>
    Trial,

    /// <summary>Quests that reward passive skill points.</summary>
    Passive,

    /// <summary>Crafting bench recipes worth picking up while levelling.</summary>
    Recipe,

    /// <summary>Other quests that help the campaign (the Library, Quicksilver Flask, ...).</summary>
    Quest,
}

/// <summary>One line on the overlay.</summary>
/// <param name="Id">Stable identifier, unique across the guide.</param>
/// <param name="Kind">Which section it belongs in.</param>
/// <param name="Areas">Area IDs where the goal is done; entering one highlights the goal.</param>
/// <param name="Text">The goal itself, e.g. "황혼의 해안에서 힐록 처치".</param>
/// <param name="Note">Optional second line: who rewards it, how to get there.</param>
/// <param name="Key">Marks an especially important entry (shown with a star).</param>
public sealed record Goal(
    string Id,
    GoalKind Kind,
    IReadOnlyList<string> Areas,
    string Text,
    string? Note = null,
    bool Key = false);

/// <summary>All goals of one act.</summary>
public sealed record ActGuide(int Act, IReadOnlyList<Goal> Goals);

/// <summary>A world area as the Client.txt log identifies it.</summary>
/// <param name="Id">The internal area ID, e.g. <c>1_1_2</c>.</param>
/// <param name="Name">Display name (Korean).</param>
/// <param name="NameEn">English name, for reference.</param>
/// <param name="Act">Act 1–10.</param>
/// <param name="Level">Monster level of the area.</param>
/// <param name="Town">True for towns.</param>
public sealed record AreaInfo(string Id, string Name, string NameEn, int Act, int Level, bool Town);

/// <summary>The whole guide: goals per act, plus the area table used for detection.</summary>
public sealed class GuideData
{
    /// <summary>First and last act the guide covers.</summary>
    public const int FirstAct = 1;

    /// <inheritdoc cref="FirstAct"/>
    public const int LastAct = 10;

    /// <summary>Creates the guide.</summary>
    /// <param name="acts">Goals per act.</param>
    /// <param name="areas">Area table keyed by area ID.</param>
    public GuideData(IReadOnlyList<ActGuide> acts, IReadOnlyDictionary<string, AreaInfo> areas)
    {
        Acts = acts;
        Areas = areas;
    }

    /// <summary>Goals per act, in act order.</summary>
    public IReadOnlyList<ActGuide> Acts { get; }

    /// <summary>Area table keyed by area ID.</summary>
    public IReadOnlyDictionary<string, AreaInfo> Areas { get; }

    /// <summary>The goals of <paramref name="act"/>, or an empty guide when the act has none.</summary>
    /// <param name="act">Act number.</param>
    /// <returns>The act's guide.</returns>
    public ActGuide GetAct(int act) =>
        Acts.FirstOrDefault(a => a.Act == act) ?? new ActGuide(act, Array.Empty<Goal>());
}
