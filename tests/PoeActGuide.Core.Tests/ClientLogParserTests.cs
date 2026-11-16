using System.Text;
using PoeActGuide.Core.Log;

namespace PoeActGuide.Core.Tests;

public class ClientLogParserTests
{
    private const string CoastLine =
        "2024/12/01 20:15:03 123456 cff94598 [DEBUG Client 1234] Generating level 2 area \"1_1_2\" with seed 2914721377";

    [Fact]
    public void Parses_area_generation_line()
    {
        Assert.True(ClientLogParser.TryParseAreaEntered(CoastLine, out var entered));
        Assert.Equal(new AreaEntered("1_1_2", 2), entered);
    }

    [Theory]
    [InlineData("2024/12/01 20:15:04 123457 cff94598 [INFO Client 1234] : 해안 지대에 진입했습니다.")]
    [InlineData("2024/12/01 20:15:04 123457 cff94598 [INFO Client 1234] : You have entered The Coast.")]
    [InlineData("2024/12/01 20:15:04 123457 cff94598 [INFO Client 1234] @From Someone: Generating level 2 area")]
    [InlineData("")]
    public void Ignores_other_lines(string line)
    {
        Assert.False(ClientLogParser.TryParseAreaEntered(line, out var entered));
        Assert.Null(entered);
    }

    [Fact]
    public void Parses_hideout_and_part_two_ids()
    {
        Assert.True(ClientLogParser.TryParseAreaEntered(
            "x [DEBUG Client 1] Generating level 45 area \"2_7_5_map\" with seed 1", out var map));
        Assert.Equal("2_7_5_map", map!.AreaId);
        Assert.Equal(45, map.Level);

        Assert.True(ClientLogParser.TryParseAreaEntered(
            "x [DEBUG Client 1] Generating level 1 area \"HideoutFelled\" with seed 1", out var hideout));
        Assert.Equal("HideoutFelled", hideout!.AreaId);
    }

    [Fact]
    public void Finds_last_area_in_stream()
    {
        var log = string.Join('\n',
            CoastLine,
            "noise",
            "x [DEBUG Client 1] Generating level 3 area \"1_1_2a\" with seed 9",
            "x [INFO Client 1] : 물결 섬에 진입했습니다.",
            string.Empty);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(log));

        Assert.Equal(new AreaEntered("1_1_2a", 3), ClientLogParser.FindLastAreaEntered(stream));
    }

    [Fact]
    public void Tail_scan_skips_the_cut_first_line()
    {
        // The scan window starts in the middle of the area line, so that line must not be parsed.
        var early = "x [DEBUG Client 1] Generating level 3 area \"1_1_2a\" with seed 9\n";
        var cut = "x [DEBUG Client 1] Generating level 2 area \"1_1_2\" with seed 1\n";
        var tail = "plain line\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(early + cut + tail));
        // The window starts three bytes into `cut`: the rest of that line still reads as a valid area
        // line ("DEBUG Client 1] Generating level 2 area ..."), so only dropping it gives null.
        var window = tail.Length + cut.Length - 3;

        Assert.Null(ClientLogParser.FindLastAreaEntered(stream, window));
    }

    [Fact]
    public void Missing_file_yields_null()
    {
        Assert.Null(ClientLogParser.FindLastAreaEntered(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")));
    }
}
