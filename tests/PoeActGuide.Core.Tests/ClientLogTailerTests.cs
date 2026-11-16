using System.Text;
using PoeActGuide.Core.Log;

namespace PoeActGuide.Core.Tests;

public sealed class ClientLogTailerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"poe-act-guide-{Guid.NewGuid()}.txt");

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void Starting_at_end_skips_existing_content()
    {
        File.WriteAllText(_path, "old 1\nold 2\n");
        using var tailer = new ClientLogTailer(_path, startAtEnd: true);

        Append("new\n");

        Assert.Equal(new[] { "new" }, tailer.ReadNewLines());
    }

    [Fact]
    public void Starting_at_beginning_reads_existing_content()
    {
        File.WriteAllText(_path, "old 1\r\nold 2\n");
        using var tailer = new ClientLogTailer(_path, startAtEnd: false);

        Assert.Equal(new[] { "old 1", "old 2" }, tailer.ReadNewLines());
        Assert.Empty(tailer.ReadNewLines());
    }

    [Fact]
    public void Partial_line_is_held_until_its_newline_arrives()
    {
        File.WriteAllText(_path, string.Empty);
        using var tailer = new ClientLogTailer(_path);

        Append("Generating level 2 ar");
        Assert.Empty(tailer.ReadNewLines());

        Append("ea \"1_1_2\"\nnext");
        Assert.Equal(new[] { "Generating level 2 area \"1_1_2\"" }, tailer.ReadNewLines());
    }

    [Fact]
    public void Multibyte_character_split_across_reads_is_decoded_once()
    {
        File.WriteAllText(_path, string.Empty);
        using var tailer = new ClientLogTailer(_path);
        var bytes = Encoding.UTF8.GetBytes("해안\n");

        AppendBytes(bytes[..1]); // first byte of '해'
        Assert.Empty(tailer.ReadNewLines());
        AppendBytes(bytes[1..]);

        Assert.Equal(new[] { "해안" }, tailer.ReadNewLines());
    }

    [Fact]
    public void Truncated_file_is_read_again_from_start()
    {
        File.WriteAllText(_path, "a long first session line\n");
        using var tailer = new ClientLogTailer(_path, startAtEnd: true);

        File.WriteAllText(_path, "fresh\n");

        Assert.Equal(new[] { "fresh" }, tailer.ReadNewLines());
    }

    [Fact]
    public void Missing_file_reads_nothing_and_is_picked_up_when_created()
    {
        using var tailer = new ClientLogTailer(_path, startAtEnd: true);
        Assert.Empty(tailer.ReadNewLines());

        File.WriteAllText(_path, "created\n");

        Assert.Equal(new[] { "created" }, tailer.ReadNewLines());
    }

    [Fact]
    public async Task Started_tailer_raises_line_events()
    {
        File.WriteAllText(_path, string.Empty);
        using var tailer = new ClientLogTailer(_path);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        tailer.LineAppended += (_, line) => received.TrySetResult(line);
        tailer.Start(TimeSpan.FromMilliseconds(20));

        Append("hello\n");

        Assert.Equal("hello", await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private void Append(string text) => AppendBytes(Encoding.UTF8.GetBytes(text));

    private void AppendBytes(byte[] bytes)
    {
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        stream.Write(bytes);
    }
}
