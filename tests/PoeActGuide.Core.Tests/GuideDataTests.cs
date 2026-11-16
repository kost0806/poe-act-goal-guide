using PoeActGuide.Core.Guide;

namespace PoeActGuide.Core.Tests;

public class GuideDataTests
{
    [Fact]
    public void Shipped_guide_has_no_validation_problems()
    {
        var problems = GuideValidator.Validate(TestData.ShippedGuide);

        Assert.Empty(problems);
    }

    [Fact]
    public void Every_act_has_main_goals_and_at_least_one_trial_or_passive_goal()
    {
        var data = TestData.ShippedGuide;

        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            var goals = data.GetAct(act).Goals;
            Assert.Contains(goals, g => g.Kind == GoalKind.Main);
            Assert.Contains(goals, g => g.Kind is GoalKind.Trial or GoalKind.Passive);
        }
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
    public void Trials_cover_the_six_normal_trials_of_acts_one_to_three()
    {
        var trialAreas = Enumerable.Range(1, 3)
            .SelectMany(act => TestData.ShippedGuide.GetAct(act).Goals)
            .Where(g => g.Kind == GoalKind.Trial && !g.Areas.Contains("Labyrinth_Airlock"))
            .SelectMany(g => g.Areas)
            .ToHashSet();

        // Lower Prison, Chamber of Sins 2, Crypt 1, Crematorium, Catacombs, Imperial Gardens.
        Assert.Superset(new HashSet<string> { "1_1_7_1", "1_2_6_2", "1_2_5_1", "1_3_3_1", "1_3_6", "1_3_15" }, trialAreas);
    }

    [Fact]
    public void Validator_reports_unknown_areas_and_duplicate_ids()
    {
        var areas = new Dictionary<string, AreaInfo> { ["a"] = new("a", "A", "A", 1, 1, false) };
        var acts = Enumerable.Range(1, 10)
            .Select(n => new ActGuide(n, n == 1
                ? new[]
                {
                    new Goal("dup", GoalKind.Main, new[] { "a" }, "ok"),
                    new Goal("dup", GoalKind.Main, new[] { "nowhere" }, "bad"),
                }
                : Array.Empty<Goal>()))
            .ToList();

        var problems = GuideValidator.Validate(new GuideData(acts, areas));

        Assert.Contains(problems, p => p.Contains("중복된 목표 id: dup", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("'nowhere'", StringComparison.Ordinal));
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
            File.WriteAllText(Path.Combine(dir, GuideLoader.GuideFileName), "{ \"acts\": [ { \"act\": 1, \"goals\": [ { \"kind\": \"nonsense\" } ] } ] }");
            File.WriteAllText(Path.Combine(dir, GuideLoader.AreasFileName), "{}");

            Assert.Throws<GuideDataException>(() => GuideLoader.LoadFromDirectory(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
