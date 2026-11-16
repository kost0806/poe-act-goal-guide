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

        for (var act = GuideData.FirstAct; act <= GuideData.LastAct; act++)
        {
            var count = data.Acts.Count(a => a.Act == act);
            if (count != 1)
            {
                problems.Add($"{act}장 항목이 정확히 하나가 아닙니다 ({count}개).");
            }
        }

        foreach (var act in data.Acts)
        {
            if (act.Act is < GuideData.FirstAct or > GuideData.LastAct)
            {
                problems.Add($"알 수 없는 액트 번호: {act.Act}");
            }

            for (var i = 0; i < act.Steps.Count; i++)
            {
                var step = act.Steps[i];
                var where = $"{act.Act}장 {i + 1}번째 단계";

                if (string.IsNullOrWhiteSpace(step.Text))
                {
                    problems.Add($"{where}: text 가 비어 있습니다.");
                }

                if (!Enum.IsDefined(step.Kind))
                {
                    problems.Add($"{where}: 알 수 없는 kind {step.Kind}.");
                }

                if (step.Areas.Count == 0)
                {
                    problems.Add($"{where}: areas 가 비어 있습니다.");
                }

                foreach (var area in step.Areas)
                {
                    if (!data.Areas.ContainsKey(area))
                    {
                        problems.Add($"{where}: 지역 표에 없는 지역 ID '{area}'.");
                    }
                }
            }
        }

        return problems;
    }
}
