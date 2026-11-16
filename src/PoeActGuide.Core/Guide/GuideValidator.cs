namespace PoeActGuide.Core.Guide;

/// <summary>Consistency checks for hand-edited guide data.</summary>
public static class GuideValidator
{
    /// <summary>Lists every problem found; empty when the guide is consistent.</summary>
    /// <param name="data">The loaded guide.</param>
    /// <returns>Human-readable problems.</returns>
    public static IReadOnlyList<string> Validate(GuideData data)
    {
        var problems = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            if (data.Acts.Count(a => a.Act == act) != 1)
            {
                problems.Add($"{act}장 항목이 정확히 하나가 아닙니다.");
            }
        }

        foreach (var act in data.Acts)
        {
            if (act.Act is < GuideData.FirstAct or > GuideData.LastAct)
            {
                problems.Add($"알 수 없는 액트 번호: {act.Act}");
            }

            foreach (var goal in act.Goals)
            {
                if (string.IsNullOrWhiteSpace(goal.Id))
                {
                    problems.Add($"{act.Act}장: id 가 없는 목표가 있습니다 ('{goal.Text}').");
                }
                else if (!seenIds.Add(goal.Id))
                {
                    problems.Add($"중복된 목표 id: {goal.Id}");
                }

                if (string.IsNullOrWhiteSpace(goal.Text))
                {
                    problems.Add($"{goal.Id}: text 가 비어 있습니다.");
                }

                if (!Enum.IsDefined(goal.Kind))
                {
                    problems.Add($"{goal.Id}: 알 수 없는 kind {goal.Kind}.");
                }

                if (goal.Areas.Count == 0)
                {
                    problems.Add($"{goal.Id}: areas 가 비어 있습니다.");
                }

                foreach (var area in goal.Areas)
                {
                    if (!data.Areas.ContainsKey(area))
                    {
                        problems.Add($"{goal.Id}: 지역 표에 없는 지역 ID '{area}'.");
                    }
                }
            }
        }

        return problems;
    }
}
