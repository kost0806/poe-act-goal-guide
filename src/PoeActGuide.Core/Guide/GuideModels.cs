namespace PoeActGuide.Core.Guide;

/// <summary>What sort of step a route entry is. The overlay colours and tags steps by kind.</summary>
public enum StepKind
{
    /// <summary>The act's main progression — "kill X in Y". Tag: 진행.</summary>
    Main,

    /// <summary>Trials of Ascendancy and the Labyrinth. Tag: 전직.</summary>
    Trial,

    /// <summary>Quests that reward passive skill points. Tag: 패시브.</summary>
    Passive,

    /// <summary>Crafting bench options unlocked in an area. Tag: 작업대.</summary>
    Recipe,

    /// <summary>Other quests that help the campaign (the Library, Medicine Chest, ...). Tag: 퀘스트.</summary>
    Quest,
}

/// <summary>One step of the levelling route.</summary>
/// <param name="Kind">What sort of step it is.</param>
/// <param name="Areas">Area IDs where the step is done; entering one moves the route to this step.</param>
/// <param name="Text">The step itself, e.g. "황혼의 해안: 힐록 처치".</param>
/// <param name="Note">Optional second line: directions, who rewards it, the quest name.</param>
/// <param name="Key">Marks an especially important step (shown with a star).</param>
public sealed record RouteStep(
    StepKind Kind,
    IReadOnlyList<string> Areas,
    string Text,
    string? Note = null,
    bool Key = false);

/// <summary>The route through one act, in order.</summary>
public sealed record ActGuide(int Act, IReadOnlyList<RouteStep> Steps);

/// <summary>A world area as the Client.txt log identifies it.</summary>
/// <param name="Id">The internal area ID, e.g. <c>1_1_2</c>.</param>
/// <param name="Name">Display name (Korean client).</param>
/// <param name="NameEn">English name, for reference.</param>
/// <param name="Act">Act 1–10.</param>
/// <param name="Level">Monster level of the area.</param>
/// <param name="Town">True for towns.</param>
public sealed record AreaInfo(string Id, string Name, string NameEn, int Act, int Level, bool Town);

/// <summary>The whole guide: the route per act, plus the area table used for detection.</summary>
public sealed class GuideData
{
    /// <summary>First and last act the guide covers.</summary>
    public const int FirstAct = 1;

    /// <inheritdoc cref="FirstAct"/>
    public const int LastAct = 10;

    /// <summary>Creates the guide.</summary>
    /// <param name="acts">Route per act.</param>
    /// <param name="areas">Area table keyed by area ID.</param>
    public GuideData(IReadOnlyList<ActGuide> acts, IReadOnlyDictionary<string, AreaInfo> areas)
    {
        Acts = acts;
        Areas = areas;
    }

    /// <summary>Route per act, in act order.</summary>
    public IReadOnlyList<ActGuide> Acts { get; }

    /// <summary>Area table keyed by area ID.</summary>
    public IReadOnlyDictionary<string, AreaInfo> Areas { get; }

    /// <summary>The route of <paramref name="act"/>, or an empty one when the act has none.</summary>
    /// <param name="act">Act number.</param>
    /// <returns>The act's route.</returns>
    public ActGuide GetAct(int act) =>
        Acts.FirstOrDefault(a => a.Act == act) ?? new ActGuide(act, Array.Empty<RouteStep>());
}
