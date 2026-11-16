using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Tests;

internal static class TestData
{
    private static readonly Lazy<GuideData> Shipped = new(() =>
        GuideLoader.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "Data")));

    /// <summary>The real data/*.json files, copied next to the test assembly.</summary>
    public static GuideData ShippedGuide => Shipped.Value;

    /// <summary>A tiny hand-made guide for tracker tests.</summary>
    public static GuideData Small()
    {
        var areas = new[]
        {
            new AreaInfo("1_1_1", "황혼의 해안", "The Twilight Strand", 1, 1, false),
            new AreaInfo("1_1_town", "라이온아이 초소", "Lioneye's Watch", 1, 13, true),
            new AreaInfo("1_2_1", "남쪽 숲", "The Southern Forest", 2, 13, false),
            new AreaInfo("1_3_town", "사안 야영지", "The Sarn Encampment", 3, 33, true),
            new AreaInfo("Labyrinth_Airlock", "지망자의 광장", "Aspirants' Plaza", 3, 33, false),
            new AreaInfo("1_4_1", "수로", "The Aqueduct", 4, 33, false),
        }.ToDictionary(a => a.Id);

        var acts = new List<ActGuide>
        {
            new(1, new[]
            {
                new Goal("a1-main", GoalKind.Main, new[] { "1_1_1" }, "힐록 처치"),
                new Goal("a1-trial", GoalKind.Trial, new[] { "1_1_town" }, "시험"),
                new Goal("a1-recipe", GoalKind.Recipe, new[] { "1_1_town" }, "조합법"),
                new Goal("a1-key", GoalKind.Quest, new[] { "1_2_1" }, "중요 퀘스트", Key: true),
                new Goal("a1-quest", GoalKind.Quest, new[] { "1_2_1" }, "보통 퀘스트"),
            }),
            new(2, new[] { new Goal("a2-main", GoalKind.Main, new[] { "1_2_1" }, "2장 진행") }),
        };

        return new GuideData(acts, areas);
    }
}
