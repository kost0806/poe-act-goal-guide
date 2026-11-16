using PoeActGuide.Core.Guide;
using PoeActGuide.Core.Progress;

namespace PoeActGuide.Core.Tests;

public class ProgressTrackerTests
{
    private static ProgressTracker Walk(params string[] areas)
    {
        var tracker = new ProgressTracker(TestData.Small());
        foreach (var area in areas)
        {
            tracker.OnAreaEntered(area);
        }

        return tracker;
    }

    [Fact]
    public void Starts_on_act_one_before_the_first_step()
    {
        var view = new ProgressTracker(TestData.Small()).BuildView();

        Assert.Equal(1, view.Act);
        Assert.Equal(0, view.CurrentNumber);
        Assert.Equal(8, view.TotalSteps);
        Assert.All(view.Steps, s => Assert.Equal(StepState.Upcoming, s.State));
    }

    [Fact]
    public void Entering_areas_in_order_follows_the_route()
    {
        var tracker = Walk("1_1_1", "1_1_town", "1_1_2", "1_1_3");

        var view = tracker.BuildView();
        Assert.Equal(3, view.CurrentNumber);
        Assert.Equal(new[] { StepState.Done, StepState.Done, StepState.Current, StepState.Upcoming }, view.Steps.Take(4).Select(s => s.State));
        Assert.Equal("갯벌", view.CurrentArea?.Name);
    }

    [Fact]
    public void Going_back_to_an_earlier_area_does_not_move_backwards()
    {
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3", "1_1_4");

        tracker.OnAreaEntered("1_1_town");

        Assert.Equal(3, tracker.PositionOf(1));
    }

    [Fact]
    public void Area_further_than_the_lookahead_does_not_jump()
    {
        // Step 7 lists 1_1_2 again, but from step 1 it is 6 steps ahead.
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3");

        tracker.OnAreaEntered("1_1_2");

        Assert.Equal(2, tracker.PositionOf(1));
    }

    [Fact]
    public void Area_within_the_lookahead_skips_ahead()
    {
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3");

        tracker.OnAreaEntered("1_1_6"); // 3 steps ahead of step 2

        Assert.Equal(5, tracker.PositionOf(1));
    }

    [Fact]
    public void Entering_the_next_act_switches_act_and_starts_its_route()
    {
        var tracker = Walk("1_1_1", "1_2_1");

        Assert.Equal(2, tracker.DisplayedAct);
        Assert.Equal(0, tracker.PositionOf(2));
        Assert.Equal(0, tracker.PositionOf(1));
    }

    [Fact]
    public void Route_step_in_an_earlier_act_keeps_the_current_act()
    {
        // Act 2 step 3 sends the player back to the act 1 town.
        var tracker = Walk("1_2_1", "1_2_9");

        tracker.OnAreaEntered("1_1_town");

        Assert.Equal(2, tracker.DisplayedAct);
        Assert.Equal(2, tracker.PositionOf(2));
    }

    [Fact]
    public void Town_only_advances_to_the_very_next_step()
    {
        // Act 2: the act 1 town is step 3, two steps ahead of step 1 — too far for a town.
        var tracker = Walk("1_2_1");

        tracker.OnAreaEntered("1_1_town");

        Assert.Equal(1, tracker.DisplayedAct);
        Assert.Equal(0, tracker.PositionOf(2));
    }

    [Fact]
    public void Shipped_route_does_not_skip_to_the_bandit_hand_in_when_visiting_town()
    {
        var tracker = new ProgressTracker(TestData.ShippedGuide);
        foreach (var area in new[] { "1_2_1", "1_2_3", "1_2_6_1", "1_2_15", "1_2_5_2", "1_2_7", "1_2_10", "1_2_4" })
        {
            tracker.OnAreaEntered(area);
        }

        tracker.OnAreaEntered("1_2_town"); // after Kraityn; the Eramir hand-in is 4 steps ahead

        Assert.Equal("부서진 다리: 흉터 전달자 크레이틴 처치", tracker.BuildView().Steps.Single(s => s.State == StepState.Current).Step.Text);
    }

    [Fact]
    public void Labyrinth_entrance_never_switches_act()
    {
        var tracker = Walk("1_4_1");

        tracker.OnAreaEntered("Labyrinth_Airlock");

        Assert.Equal(4, tracker.DisplayedAct);
        Assert.Equal("Labyrinth_Airlock", tracker.CurrentAreaId);
    }

    [Fact]
    public void Unknown_area_keeps_act_and_position()
    {
        var tracker = Walk("1_2_1", "1_2_9");

        tracker.OnAreaEntered("HideoutFelled");

        var view = tracker.BuildView();
        Assert.Equal(2, view.Act);
        Assert.Equal(2, view.CurrentNumber);
        Assert.Null(view.CurrentArea);
    }

    [Fact]
    public void Returning_to_an_act_whose_position_is_stale_starts_its_route_over()
    {
        // An old character finished act 1; a new one appears in act 1 again via another act first.
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3", "1_1_4", "1_1_5", "1_1_6", "1_1_7", "1_2_1");

        tracker.OnAreaEntered("1_1_3"); // behind act 1's position and not a route step of act 2

        Assert.Equal(1, tracker.DisplayedAct);
        Assert.Equal(2, tracker.PositionOf(1));
    }

    [Fact]
    public void Twilight_strand_resets_every_act()
    {
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3", "1_2_1", "1_2_9");

        tracker.OnAreaEntered("1_1_1");

        Assert.Equal(1, tracker.DisplayedAct);
        Assert.Equal(0, tracker.PositionOf(1));
        Assert.Equal(-1, tracker.PositionOf(2));
    }

    [Fact]
    public void Manual_step_moves_are_clamped()
    {
        var tracker = Walk("1_1_1");

        tracker.PreviousStep();
        tracker.PreviousStep();
        Assert.Equal(-1, tracker.PositionOf(1));

        for (var i = 0; i < 20; i++)
        {
            tracker.NextStep();
        }

        Assert.Equal(7, tracker.PositionOf(1));
    }

    [Fact]
    public void Leaving_an_area_passes_its_consecutive_steps_only_when_the_route_continues()
    {
        var data = new GuideData(
            new[]
            {
                new ActGuide(1, new[]
                {
                    new RouteStep(StepKind.Main, new[] { "1_1_2" }, "해안 1"),
                    new RouteStep(StepKind.Main, new[] { "1_1_2" }, "해안 2"),
                    new RouteStep(StepKind.Main, new[] { "1_1_town" }, "마을 보고"),
                }),
            },
            TestData.Small().Areas);
        var tracker = new ProgressTracker(data);
        tracker.OnAreaEntered("1_1_2");

        tracker.OnAreaEntered("HideoutFelled"); // not on the route: nothing is passed
        Assert.Equal(0, tracker.PositionOf(1));

        tracker.OnAreaEntered("1_1_2");
        tracker.OnAreaEntered("1_1_town"); // both coast steps passed, town is now the next step
        Assert.Equal(2, tracker.PositionOf(1));
    }

    [Fact]
    public void Steps_in_the_current_area_are_marked_here()
    {
        var tracker = Walk("1_1_1", "1_1_2");

        var here = tracker.BuildView().Steps.Where(s => s.IsHere).Select(s => s.Number);

        Assert.Equal(new[] { 2, 8 }, here);
    }

    [Fact]
    public void View_modes_show_a_window_around_the_current_step()
    {
        var tracker = Walk("1_1_1", "1_1_2", "1_1_3", "1_1_4");

        Assert.Equal(8, tracker.BuildView(ViewMode.Full).Steps.Count);
        Assert.Equal(new[] { 3, 4, 5, 6, 7, 8 }, tracker.BuildView(ViewMode.Normal).Steps.Select(s => s.Number));
        Assert.Equal(new[] { 4, 5, 6 }, tracker.BuildView(ViewMode.Compact).Steps.Select(s => s.Number));
    }

    [Fact]
    public void Manual_act_is_clamped_and_cleared_by_the_next_area()
    {
        var tracker = new ProgressTracker(TestData.Small());

        tracker.ShowNextAct();
        Assert.True(tracker.BuildView().IsManual);
        for (var i = 0; i < 20; i++)
        {
            tracker.ShowNextAct();
        }

        Assert.Equal(GuideData.LastAct, tracker.DisplayedAct);
        Assert.Empty(tracker.BuildView().Steps);

        tracker.OnAreaEntered("1_1_1");
        Assert.Equal(1, tracker.DisplayedAct);
        Assert.Null(tracker.ManualAct);
    }

    [Fact]
    public void Stepping_back_onto_the_detected_act_is_not_manual()
    {
        var tracker = Walk("1_2_1");

        tracker.ShowNextAct();
        tracker.ShowPreviousAct();

        Assert.Null(tracker.ManualAct);
    }

    [Fact]
    public void Tags_are_korean()
    {
        Assert.Equal(
            new[] { "진행", "전직", "패시브", "작업대", "퀘스트" },
            Enum.GetValues<StepKind>().Select(ProgressTracker.TagOf));
    }

    [Fact]
    public void Shipped_route_follows_a_normal_act_one_run()
    {
        var tracker = new ProgressTracker(TestData.ShippedGuide);
        foreach (var area in new[]
        {
            "1_1_1", "1_1_town", "1_1_2", "1_1_3", "1_1_4_1", "1_1_2", "1_1_2a", "1_1_town",
            "1_1_4_1", "1_1_4_0", "1_1_4_1", "1_1_5", "1_1_6", "1_1_7_1", "1_1_town", "1_1_7_1",
        })
        {
            tracker.OnAreaEntered(area);
        }

        var current = tracker.BuildView().Steps.Single(s => s.State == StepState.Current);
        Assert.Equal("수용소 하층의 미궁 도전", current.Step.Text);
    }

    [Fact]
    public void Shipped_route_keeps_act_two_while_visiting_lioneyes_watch_for_the_way_forward()
    {
        var tracker = new ProgressTracker(TestData.ShippedGuide);
        foreach (var area in new[]
        {
            "1_2_1", "1_2_town", "1_2_2", "1_2_2a", "1_2_town", "1_2_3", "1_2_6_1", "1_2_6_2", "1_2_town",
            "1_2_3", "1_2_15", "1_2_5_1", "1_2_5_2", "1_2_town", "1_2_7", "1_2_9", "1_2_10", "1_2_town",
            "1_2_3", "1_2_4", "1_2_town", "1_2_7", "1_2_12", "1_2_9", "1_2_town",
        })
        {
            tracker.OnAreaEntered(area);
        }

        tracker.OnAreaEntered("1_1_town");

        Assert.Equal(2, tracker.DisplayedAct);
        Assert.Equal("1장 라이온아이 초소: 베스텔에게 보고", tracker.BuildView().Steps.Single(s => s.State == StepState.Current).Step.Text);
    }

    [Fact]
    public void Shipped_route_stays_in_act_eight_through_the_blood_aqueduct_detour()
    {
        var tracker = new ProgressTracker(TestData.ShippedGuide);
        foreach (var area in new[]
        {
            "2_8_1", "2_8_town", "2_8_2_1", "2_8_2_2", "2_8_8", "2_8_9", "2_8_10", "2_8_12_1", "2_8_12_2", "2_8_town",
            "2_8_12_1", "2_8_11", "2_8_13", "2_8_6", "2_8_7_1_", "2_8_7_2", "2_8_town", "2_8_6", "2_8_13",
            "2_9_1", "2_9_town", "2_8_6", "2_8_5",
        })
        {
            tracker.OnAreaEntered(area);
        }

        Assert.Equal(8, tracker.DisplayedAct);
        Assert.Equal("목욕탕의 미궁 도전", tracker.BuildView().Steps.Single(s => s.State == StepState.Current).Step.Text);
    }
}
