using System.ComponentModel;
using System.Diagnostics;

namespace PoeActGuide.Core.Log;

/// <summary>Finds the game's log file.</summary>
public static class ClientLogLocator
{
    /// <summary>Log file names: the GGG/Steam client writes Client.txt, the Kakao client KakaoClient.txt.</summary>
    public static readonly IReadOnlyList<string> LogFileNames = new[] { "KakaoClient.txt", "Client.txt" };

    /// <summary>Common install directories, tried when the game is not running.</summary>
    public static readonly IReadOnlyList<string> DefaultInstallDirectories = new[]
    {
        @"C:\Daum Games\Path of Exile",
        @"C:\Kakao Games\Path of Exile",
        @"C:\Program Files (x86)\Grinding Gear Games\Path of Exile",
        @"C:\Program Files\Grinding Gear Games\Path of Exile",
        @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile",
        @"C:\Program Files\Steam\steamapps\common\Path of Exile",
    };

    private static readonly string[] GameProcessNames =
    {
        "PathOfExile", "PathOfExile_x64",
        "PathOfExile_KG", "PathOfExile_x64_KG",
        "PathOfExileSteam", "PathOfExile_x64Steam",
        "PathOfExileEGS", "PathOfExile_x64EGS",
    };

    /// <summary>Locates the log file on this machine.</summary>
    /// <param name="configuredPath">A path set by the user; used whenever it exists.</param>
    /// <returns>The log path, or null when none was found.</returns>
    public static string? Locate(string? configuredPath) =>
        Locate(configuredPath, RunningGameDirectories().Concat(DefaultInstallDirectories), LastWriteOrNull);

    /// <summary>Locates the log file from the given candidates.</summary>
    /// <param name="configuredPath">A path set by the user; used whenever it exists.</param>
    /// <param name="installDirectories">Game directories to look in, most likely first.</param>
    /// <param name="lastWrite">Last write time of a file, or null when it does not exist.</param>
    /// <returns>
    /// The configured path if it exists; otherwise, among the candidates found in the first directory
    /// that has any, the most recently written; null when nothing exists.
    /// </returns>
    public static string? Locate(string? configuredPath, IEnumerable<string> installDirectories, Func<string, DateTime?> lastWrite)
    {
        ArgumentNullException.ThrowIfNull(installDirectories);
        ArgumentNullException.ThrowIfNull(lastWrite);

        if (!string.IsNullOrWhiteSpace(configuredPath) && lastWrite(configuredPath) is not null)
        {
            return configuredPath;
        }

        foreach (var dir in installDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var found = LogFileNames
                .Select(name => System.IO.Path.Combine(dir, "logs", name))
                .Select(path => (path, time: lastWrite(path)))
                .Where(c => c.time is not null)
                .OrderByDescending(c => c.time)
                .Select(c => c.path)
                .FirstOrDefault();
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Directories of running game processes, when their image path can be read.</summary>
    /// <returns>Install directories of running clients.</returns>
    public static IReadOnlyList<string> RunningGameDirectories()
    {
        var dirs = new List<string>();
        foreach (var name in GameProcessNames)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(name);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        var file = process.MainModule?.FileName;
                        var dir = file is null ? null : System.IO.Path.GetDirectoryName(file);
                        if (dir is not null)
                        {
                            dirs.Add(dir);
                        }
                    }
                    catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException)
                    {
                        // Access denied (e.g. the game runs elevated) or the process just exited.
                    }
                }
            }
        }

        return dirs;
    }

    private static DateTime? LastWriteOrNull(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.LastWriteTimeUtc : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
