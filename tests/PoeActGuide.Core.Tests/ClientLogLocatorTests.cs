using PoeActGuide.Core.Log;

namespace PoeActGuide.Core.Tests;

public class ClientLogLocatorTests
{
    private static readonly DateTime Old = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime New = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Configured_path_wins_when_it_exists()
    {
        var files = new Dictionary<string, DateTime>
        {
            ["D:/custom/Client.txt"] = Old,
            [Log("C:/poe", "Client.txt")] = New,
        };

        Assert.Equal("D:/custom/Client.txt", Locate("D:/custom/Client.txt", new[] { "C:/poe" }, files));
    }

    [Fact]
    public void Missing_configured_path_falls_back_to_detection()
    {
        var files = new Dictionary<string, DateTime> { [Log("C:/poe", "Client.txt")] = New };

        Assert.Equal(Log("C:/poe", "Client.txt"), Locate("D:/gone/Client.txt", new[] { "C:/poe" }, files));
    }

    [Fact]
    public void First_directory_with_a_log_wins()
    {
        var files = new Dictionary<string, DateTime>
        {
            [Log("C:/kakao", "KakaoClient.txt")] = Old,
            [Log("C:/steam", "Client.txt")] = New,
        };

        Assert.Equal(Log("C:/kakao", "KakaoClient.txt"), Locate(null, new[] { "C:/empty", "C:/kakao", "C:/steam" }, files));
    }

    [Fact]
    public void Newest_log_wins_inside_one_directory()
    {
        var files = new Dictionary<string, DateTime>
        {
            [Log("C:/poe", "KakaoClient.txt")] = Old,
            [Log("C:/poe", "Client.txt")] = New,
        };

        Assert.Equal(Log("C:/poe", "Client.txt"), Locate(null, new[] { "C:/poe" }, files));
    }

    [Fact]
    public void Nothing_found_yields_null()
    {
        Assert.Null(Locate(null, new[] { "C:/poe" }, new Dictionary<string, DateTime>()));
    }

    private static string Log(string dir, string name) => Path.Combine(dir, "logs", name);

    private static string? Locate(string? configured, IEnumerable<string> dirs, Dictionary<string, DateTime> files) =>
        ClientLogLocator.Locate(configured, dirs, p => files.TryGetValue(p, out var t) ? t : null);
}
