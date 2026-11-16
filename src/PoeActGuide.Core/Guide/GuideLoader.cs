using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoeActGuide.Core.Guide;

/// <summary>A guide file is missing or malformed. The message is meant for the user.</summary>
public sealed class GuideDataException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">User-facing message.</param>
    /// <param name="inner">The underlying cause, if any.</param>
    public GuideDataException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>Reads <c>guide.ko.json</c> and <c>areas.ko.json</c>.</summary>
public static class GuideLoader
{
    /// <summary>File name of the goal list.</summary>
    public const string GuideFileName = "guide.ko.json";

    /// <summary>File name of the area table.</summary>
    public const string AreasFileName = "areas.ko.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Loads both files from <paramref name="dataDirectory"/>.</summary>
    /// <param name="dataDirectory">Directory holding the two JSON files.</param>
    /// <returns>The loaded guide.</returns>
    /// <exception cref="GuideDataException">A file is missing or cannot be parsed.</exception>
    public static GuideData LoadFromDirectory(string dataDirectory) =>
        Load(Path.Combine(dataDirectory, GuideFileName), Path.Combine(dataDirectory, AreasFileName));

    /// <summary>Loads the guide from explicit paths.</summary>
    /// <param name="guidePath">Path of the goal list.</param>
    /// <param name="areasPath">Path of the area table.</param>
    /// <returns>The loaded guide.</returns>
    /// <exception cref="GuideDataException">A file is missing or cannot be parsed.</exception>
    public static GuideData Load(string guidePath, string areasPath)
    {
        var guide = Read<GuideFile>(guidePath);
        var areas = Read<Dictionary<string, AreaFileEntry>>(areasPath);

        var acts = (guide.Acts ?? new List<ActFileEntry>())
            .OrderBy(a => a.Act)
            .Select(a => new ActGuide(a.Act, (a.Goals ?? new List<GoalFileEntry>()).Select(ToGoal).ToList()))
            .ToList();

        var areaTable = areas.ToDictionary(
            kv => kv.Key,
            kv => new AreaInfo(kv.Key, kv.Value.Name ?? kv.Key, kv.Value.NameEn ?? string.Empty, kv.Value.Act, kv.Value.Level, kv.Value.Town),
            StringComparer.Ordinal);

        return new GuideData(acts, areaTable);
    }

    private static Goal ToGoal(GoalFileEntry g) =>
        new(g.Id ?? string.Empty, g.Kind, g.Areas ?? new List<string>(), g.Text ?? string.Empty, g.Note, g.Key);

    private static T Read<T>(string path)
        where T : class
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new GuideDataException($"가이드 데이터 파일을 읽을 수 없습니다: {path}", e);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new GuideDataException($"가이드 데이터 파일이 비어 있습니다: {path}");
        }
        catch (JsonException e)
        {
            throw new GuideDataException($"가이드 데이터 파일 형식이 잘못되었습니다: {path}\n{e.Message}", e);
        }
    }

    private sealed class GuideFile
    {
        public List<ActFileEntry>? Acts { get; set; }
    }

    private sealed class ActFileEntry
    {
        public int Act { get; set; }

        public List<GoalFileEntry>? Goals { get; set; }
    }

    private sealed class GoalFileEntry
    {
        public string? Id { get; set; }

        public GoalKind Kind { get; set; }

        public List<string>? Areas { get; set; }

        public string? Text { get; set; }

        public string? Note { get; set; }

        public bool Key { get; set; }
    }

    private sealed class AreaFileEntry
    {
        public string? Name { get; set; }

        public string? NameEn { get; set; }

        public int Act { get; set; }

        public int Level { get; set; }

        public bool Town { get; set; }
    }
}
