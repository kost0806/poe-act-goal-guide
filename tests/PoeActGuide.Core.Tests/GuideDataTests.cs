using System.Text.RegularExpressions;
using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Tests;

public class GuideDataTests
{
    [Fact]
    public void Shipped_guide_has_no_validation_problems()
    {
        Assert.Empty(GuideValidator.Validate(TestData.ShippedGuide));
    }

    [Fact]
    public void Every_act_route_has_main_steps_and_a_trial_or_passive_step()
    {
        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            var steps = TestData.ShippedGuide.GetAct(act).Steps;
            Assert.Contains(steps, s => s.Kind == StepKind.Main);
            Assert.Contains(steps, s => s.Kind is StepKind.Trial or StepKind.Passive);
        }
    }

    [Fact]
    public void Every_act_route_starts_in_that_act()
    {
        var data = TestData.ShippedGuide;
        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            var first = data.GetAct(act).Steps[0];
            Assert.Contains(first.Areas, id => data.Areas[id].Act == act);
        }
    }

    [Fact]
    public void Route_contains_all_trials_of_ascendancy()
    {
        var trialAreas = TestData.ShippedGuide.Acts
            .SelectMany(a => a.Steps)
            .Where(s => s.Kind == StepKind.Trial)
            .SelectMany(s => s.Areas)
            .ToHashSet();

        // Normal: Lower Prison, Chamber of Sins 2, Crypt 1, Crematorium, Catacombs, Imperial Gardens.
        // Cruel: Lower Prison, Crypt, Chamber of Sins 2. Merciless: Bath House, Tunnel, Ossuary.
        Assert.Superset(
            new HashSet<string>
            {
                "1_1_7_1", "1_2_6_2", "1_2_5_1", "1_3_3_1", "1_3_6", "1_3_15",
                "2_6_7_1", "2_7_4", "2_7_5_2",
                "2_8_5", "2_9_7", "2_10_9",
            },
            trialAreas);
    }

    [Fact]
    public void Quest_passive_points_add_up_to_twenty_four_steps()
    {
        // 24 quest passive points: 21 one-point quests + An End to Hunger (2, one step) + killing all bandits (1).
        var passiveSteps = TestData.ShippedGuide.Acts.SelectMany(a => a.Steps).Count(s => s.Kind == StepKind.Passive);

        Assert.Equal(23, passiveSteps);
    }

    [Fact]
    public void Step_texts_use_korean_terms_only()
    {
        var latin = new Regex("[A-Za-z]");
        var offenders = TestData.ShippedGuide.Acts
            .SelectMany(a => a.Steps)
            .SelectMany(s => new[] { s.Text, s.Note ?? string.Empty })
            .Select(t => t.Replace("/passives", string.Empty, StringComparison.Ordinal))
            .Where(t => latin.IsMatch(t))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Area_table_names_known_areas_in_korean()
    {
        var areas = TestData.ShippedGuide.Areas;

        Assert.Equal("해안 지대", areas["1_1_2"].Name);
        Assert.Equal(1, areas["1_1_2"].Act);
        Assert.Equal(6, areas["2_6_2"].Act);
        Assert.Equal(10, areas["2_10_town"].Act);
        Assert.True(areas["1_3_town"].Town);
    }

    [Fact]
    public void Every_act_has_exactly_one_town()
    {
        var areas = TestData.ShippedGuide.Areas.Values;

        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            Assert.Single(areas, a => a.Act == act && a.Town);
        }
    }

    [Fact]
    public void Validator_reports_unknown_areas_and_missing_acts()
    {
        var areas = new Dictionary<string, AreaInfo> { ["a"] = new("a", "A", "A", 1, 1, false) };
        var acts = Enumerable.Range(1, 9)
            .Select(n => new ActGuide(n, n == 1
                ? new[]
                {
                    new RouteStep(StepKind.Main, new[] { "a" }, "ok"),
                    new RouteStep(StepKind.Main, new[] { "nowhere" }, "bad"),
                }
                : Array.Empty<RouteStep>()))
            .ToList();

        var problems = GuideValidator.Validate(new GuideData(acts, areas));

        Assert.Contains(problems, p => p.Contains("1장 2번째 단계", StringComparison.Ordinal) && p.Contains("'nowhere'", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.StartsWith("10장", StringComparison.Ordinal));
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void Loader_reports_missing_file_as_guide_data_exception()
    {
        var dir = Path.Combine(Path.GetTempPath(), "poe-act-guide-missing-" + Guid.NewGuid());

        var ex = Assert.Throws<GuideDataException>(() => GuideLoader.LoadFromDirectory(dir));

        Assert.Contains(GuideLoader.GuideFileName, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Loader_reports_malformed_json_as_guide_data_exception()
    {
        var dir = Directory.CreateTempSubdirectory("poe-act-guide-bad-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(dir, GuideLoader.GuideFileName), "{ \"acts\": [ { \"act\": 1, \"steps\": [ { \"kind\": \"nonsense\" } ] } ] }");
            File.WriteAllText(Path.Combine(dir, GuideLoader.AreasFileName), "{}");

            Assert.Throws<GuideDataException>(() => GuideLoader.LoadFromDirectory(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
