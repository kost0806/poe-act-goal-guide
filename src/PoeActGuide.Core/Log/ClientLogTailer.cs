using System.Text;

namespace PoeActGuide.Core.Log;

/// <summary>
/// Follows a growing log file and reports each complete new line.
/// </summary>
/// <remarks>
/// The file is reopened on every poll with full sharing, so the game is never blocked and a
/// truncated or recreated log is picked up from its start.
/// </remarks>
public sealed class ClientLogTailer : IDisposable
{
    private readonly object _gate = new();
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly StringBuilder _partial = new();
    private Timer? _timer;
    private long _position;
    private int _polling;
    private bool _disposed;

    /// <summary>Creates a tailer. Nothing is read until <see cref="ReadNewLines"/> or <see cref="Start"/>.</summary>
    /// <param name="path">The log file. It may not exist yet.</param>
    /// <param name="startAtEnd">True to ignore what is already in the file.</param>
    public ClientLogTailer(string path, bool startAtEnd = true)
    {
        Path = path;
        if (startAtEnd)
        {
            try
            {
                _position = new FileInfo(path).Exists ? new FileInfo(path).Length : 0;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                _position = 0;
            }
        }
    }

    /// <summary>Raised from a thread-pool thread for every complete new line.</summary>
    public event EventHandler<string>? LineAppended;

    /// <summary>The file being followed.</summary>
    public string Path { get; }

    /// <summary>Starts polling the file.</summary>
    /// <param name="interval">Time between polls.</param>
    public void Start(TimeSpan interval)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer ??= new Timer(_ => Poll(), null, TimeSpan.Zero, interval);
    }

    /// <summary>Reads whatever complete lines were appended since the last read.</summary>
    /// <returns>New lines without their terminators; empty when nothing changed or the file is missing.</returns>
    public IReadOnlyList<string> ReadNewLines()
    {
        lock (_gate)
        {
            var lines = new List<string>();
            FileStream stream;
            try
            {
                stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return lines;
            }

            using (stream)
            {
                if (stream.Length < _position)
                {
                    // Truncated or replaced: start over.
                    _position = 0;
                    _partial.Clear();
                    _decoder.Reset();
                }

                if (stream.Length == _position)
                {
                    return lines;
                }

                stream.Seek(_position, SeekOrigin.Begin);
                var bytes = new byte[64 * 1024];
                var chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
                int read;
                while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    _position += read;
                    var count = _decoder.GetChars(bytes, 0, read, chars, 0);
                    _partial.Append(chars, 0, count);
                }
            }

            var text = _partial.ToString();
            var lastNewline = text.LastIndexOf('\n');
            if (lastNewline < 0)
            {
                return lines;
            }

            foreach (var line in text[..lastNewline].Split('\n'))
            {
                lines.Add(line.TrimEnd('\r'));
            }

            _partial.Clear();
            _partial.Append(text, lastNewline + 1, text.Length - lastNewline - 1);
            return lines;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }

    private void Poll()
    {
        if (Interlocked.Exchange(ref _polling, 1) == 1)
        {
            return;
        }

        try
        {
            foreach (var line in ReadNewLines())
            {
                LineAppended?.Invoke(this, line);
            }
        }
        finally
        {
            Volatile.Write(ref _polling, 0);
        }
    }
}
