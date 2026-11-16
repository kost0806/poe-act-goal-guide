using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Progress;

namespace PoeActGuide.Core.Tests;

public class ProgressTrackerTests
{
    [Fact]
    public void Starts_on_act_one_with_no_area()
    {
        var view = new ProgressTracker(TestData.Small()).BuildView();

        Assert.Equal(1, view.Act);
        Assert.Null(view.CurrentArea);
        Assert.False(view.IsManual);
    }

    [Fact]
    public void Entering_an_area_switches_to_its_act()
    {
        var tracker = new ProgressTracker(TestData.Small());

        tracker.OnAreaEntered("1_2_1");

        var view = tracker.BuildView();
        Assert.Equal(2, view.Act);
        Assert.Equal("남쪽 숲", view.CurrentArea?.Name);
        Assert.Equal("2장 진행", Assert.Single(Assert.Single(view.Sections).Goals).Goal.Text);
    }

    [Fact]
    public void Labyrinth_entrance_keeps_the_current_act()
    {
        var tracker = new ProgressTracker(TestData.Small());
        tracker.OnAreaEntered("1_4_1");

        tracker.OnAreaEntered("Labyrinth_Airlock");

        Assert.Equal(4, tracker.DisplayedAct);
        Assert.Equal("Labyrinth_Airlock", tracker.CurrentAreaId);
    }

    [Fact]
    public void Unknown_area_keeps_the_current_act()
    {
        var tracker = new ProgressTracker(TestData.Small());
        tracker.OnAreaEntered("1_2_1");

        tracker.OnAreaEntered("HideoutFelled");

        var view = tracker.BuildView();
        Assert.Equal(2, view.Act);
        Assert.Null(view.CurrentArea);
    }

    [Fact]
    public void Goals_in_the_current_area_are_marked_here()
    {
        var tracker = new ProgressTracker(TestData.Small());

        tracker.OnAreaEntered("1_1_town");

        var here = tracker.BuildView().Sections.SelectMany(s => s.Goals).Where(g => g.IsHere).Select(g => g.Goal.Id);
        Assert.Equal(new[] { "a1-trial", "a1-recipe" }, here);
    }

    [Fact]
    public void Sections_follow_goal_kind_order_and_carry_titles()
    {
        var view = new ProgressTracker(TestData.Small()).BuildView();

        Assert.Equal(new[] { GoalKind.Main, GoalKind.Trial, GoalKind.Recipe, GoalKind.Quest }, view.Sections.Select(s => s.Kind));
        Assert.Equal("진행", view.Sections[0].Title);
    }

    [Fact]
    public void Compact_view_keeps_main_here_and_key_goals_only()
    {
        var tracker = new ProgressTracker(TestData.Small());
        tracker.OnAreaEntered("1_1_town");
        tracker.ShowPreviousAct(); // still act 1: clamps, no manual flag

        var ids = tracker.BuildView(compact: true).Sections.SelectMany(s => s.Goals).Select(g => g.Goal.Id);

        Assert.Equal(new[] { "a1-main", "a1-trial", "a1-recipe", "a1-key" }, ids);
    }

    [Fact]
    public void Manual_act_is_clamped_and_cleared_by_the_next_area()
    {
        var tracker = new ProgressTracker(TestData.Small());
        var changes = 0;
        tracker.Changed += (_, _) => changes++;

        tracker.ShowNextAct();
        Assert.Equal(2, tracker.DisplayedAct);
        Assert.True(tracker.BuildView().IsManual);

        for (var i = 0; i < 20; i++)
        {
            tracker.ShowNextAct();
        }

        Assert.Equal(GuideData.LastAct, tracker.DisplayedAct);
        Assert.Empty(tracker.BuildView().Sections); // act 10 has no goals in the small guide

        tracker.OnAreaEntered("1_1_1");
        Assert.Equal(1, tracker.DisplayedAct);
        Assert.Null(tracker.ManualAct);
        Assert.Equal(GuideData.LastAct - 1 + 1, changes); // 9 act steps + 1 area change
    }

    [Fact]
    public void Stepping_back_onto_the_detected_act_is_not_manual()
    {
        var tracker = new ProgressTracker(TestData.Small());
        tracker.OnAreaEntered("1_2_1");

        tracker.ShowNextAct();
        tracker.ShowPreviousAct();

        Assert.Null(tracker.ManualAct);
        Assert.False(tracker.BuildView().IsManual);
    }

    [Fact]
    public void Shipped_guide_highlights_hillock_on_the_twilight_strand()
    {
        var tracker = new ProgressTracker(TestData.ShippedGuide);

        tracker.OnAreaEntered("1_1_1");

        var here = tracker.BuildView().Sections.SelectMany(s => s.Goals).Where(g => g.IsHere).ToList();
        Assert.Contains(here, g => g.Goal.Id == "a1-main-hillock");
    }
}
