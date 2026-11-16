using System.Text;
using System.Text.RegularExpressions;

namespace PoeActGuide.Core.Log;

/// <summary>The player entered (generated) a new area instance.</summary>
/// <param name="AreaId">Internal area ID, e.g. <c>1_1_2</c>.</param>
/// <param name="Level">Area level as logged.</param>
public sealed record AreaEntered(string AreaId, int Level);

/// <summary>
/// Reads area changes out of Path of Exile's <c>Client.txt</c>.
/// </summary>
/// <remarks>
/// The game writes a debug line every time it builds an area instance:
/// <code>2024/12/01 20:15:03 123456 cff94598 [DEBUG Client 1234] Generating level 2 area "1_1_2" with seed 2914721377</code>
/// Unlike the "You have entered ..." message this line is not translated, so it identifies the area
/// the same way on the Korean (Kakao), English and Steam clients.
/// </remarks>
public static partial class ClientLogParser
{
    /// <summary>How much of the end of an existing log is scanned for the last area at start-up.</summary>
    public const int DefaultTailScanBytes = 4 * 1024 * 1024;

    [GeneratedRegex("Generating level (\\d+) area \"([^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex AreaLine();

    /// <summary>Parses one log line.</summary>
    /// <param name="line">A single line of <c>Client.txt</c>.</param>
    /// <param name="entered">The area, when the line is an area-generation line.</param>
    /// <returns>True when <paramref name="line"/> announced a new area.</returns>
    public static bool TryParseAreaEntered(string line, out AreaEntered? entered)
    {
        entered = null;
        if (string.IsNullOrEmpty(line) || !line.Contains("Generating level", StringComparison.Ordinal))
        {
            return false;
        }

        var match = AreaLine().Match(line);
        if (!match.Success || !int.TryParse(match.Groups[1].ValueSpan, out var level))
        {
            return false;
        }

        entered = new AreaEntered(match.Groups[2].Value, level);
        return true;
    }

    /// <summary>Finds the most recent area in the last <paramref name="maxBytes"/> of a log file.</summary>
    /// <param name="path">Path of <c>Client.txt</c>.</param>
    /// <param name="maxBytes">How far back from the end to look.</param>
    /// <returns>The last area entered, or null when none was found or the file cannot be read.</returns>
    public static AreaEntered? FindLastAreaEntered(string path, int maxBytes = DefaultTailScanBytes)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return FindLastAreaEntered(stream, maxBytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Finds the most recent area in the last <paramref name="maxBytes"/> of a stream.</summary>
    /// <param name="stream">A readable, seekable stream over the log.</param>
    /// <param name="maxBytes">How far back from the end to look.</param>
    /// <returns>The last area entered, or null when none was found.</returns>
    public static AreaEntered? FindLastAreaEntered(Stream stream, int maxBytes = DefaultTailScanBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var start = Math.Max(0, stream.Length - maxBytes);
        stream.Seek(start, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        if (start > 0)
        {
            reader.ReadLine(); // drop the partial first line
        }

        AreaEntered? last = null;
        while (reader.ReadLine() is { } line)
        {
            if (TryParseAreaEntered(line, out var entered))
            {
                last = entered;
            }
        }

        return last;
    }
}
