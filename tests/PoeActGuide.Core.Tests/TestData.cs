using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Tests;

internal static class TestData
{
    private static readonly Lazy<GuideData> Shipped = new(() =>
        GuideLoader.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "Data")));

    /// <summary>The real data/*.json files, copied next to the test assembly.</summary>
    public static GuideData ShippedGuide => Shipped.Value;

    /// <summary>A tiny hand-made route for tracker tests.</summary>
    public static GuideData Small()
    {
        var areas = new[]
        {
            new AreaInfo("1_1_1", "황혼의 해안", "The Twilight Strand", 1, 1, false),
            new AreaInfo("1_1_town", "라이온아이 초소", "Lioneye's Watch", 1, 13, true),
            new AreaInfo("1_1_2", "해안 지대", "The Coast", 1, 2, false),
            new AreaInfo("1_1_3", "갯벌", "The Mud Flats", 1, 4, false),
            new AreaInfo("1_1_4", "가상 지역 4", "Area 4", 1, 5, false),
            new AreaInfo("1_1_5", "가상 지역 5", "Area 5", 1, 6, false),
            new AreaInfo("1_1_6", "가상 지역 6", "Area 6", 1, 7, false),
            new AreaInfo("1_1_7", "가상 지역 7", "Area 7", 1, 8, false),
            new AreaInfo("1_2_town", "숲 야영지", "The Forest Encampment", 2, 15, true),
            new AreaInfo("1_2_1", "남쪽 숲", "The Southern Forest", 2, 13, false),
            new AreaInfo("1_2_9", "서쪽 숲", "The Western Forest", 2, 16, false),
            new AreaInfo("Labyrinth_Airlock", "지망자의 광장", "Aspirants' Plaza", 3, 33, false),
            new AreaInfo("1_4_1", "수로", "The Aqueduct", 4, 33, false),
        }.ToDictionary(a => a.Id);

        static RouteStep S(StepKind kind, string text, params string[] areas) => new(kind, areas, text);

        var acts = new List<ActGuide>
        {
            new(1, new[]
            {
                S(StepKind.Main, "1 힐록", "1_1_1"),                 // 0
                S(StepKind.Main, "2 해안", "1_1_town", "1_1_2"),     // 1
                S(StepKind.Quest, "3 갯벌", "1_1_3"),                // 2
                S(StepKind.Passive, "4", "1_1_4"),                   // 3
                S(StepKind.Recipe, "5", "1_1_5"),                    // 4
                S(StepKind.Trial, "6", "1_1_6"),                     // 5
                S(StepKind.Main, "7", "1_1_7"),                      // 6
                S(StepKind.Main, "8 해안 다시", "1_1_2"),            // 7
            }),
            new(2, new[]
            {
                S(StepKind.Main, "남쪽 숲", "1_2_1", "1_2_town"),    // 0
                S(StepKind.Main, "서쪽 숲", "1_2_9"),                // 1
                S(StepKind.Passive, "1장 마을", "1_1_town"),         // 2
                S(StepKind.Trial, "미궁", "Labyrinth_Airlock"),      // 3
            }),
            new(4, new[] { S(StepKind.Main, "수로", "1_4_1") }),
        };

        return new GuideData(acts, areas);
    }
}
