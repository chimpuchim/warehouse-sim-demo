using System.IO;
using System.Text;

namespace Warehouse
{
    /// <summary>Where log lines go. The recorder writes one line per call and never reads back.</summary>
    public interface ILogSink
    {
        void WriteLine(string line);
        void Close();
    }

    /// <summary>Keeps the whole log in memory so the session can be replayed straight away (and downloaded on WebGL).</summary>
    public sealed class MemorySink : ILogSink
    {
        readonly StringBuilder _sb = new StringBuilder(1 << 16);
        public int Bytes => _sb.Length;
        public string Text => _sb.ToString();
        public void WriteLine(string line) => _sb.Append(line).Append('\n');
        public void Close() { }
    }

    /// <summary>Streams lines to a file; flushed every 50 lines so a crash loses at most one second.</summary>
    public sealed class FileSink : ILogSink
    {
        readonly StreamWriter _writer;
        int _lines;
        public string Path { get; }

        public FileSink(string path)
        {
            Path = path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            _writer = new StreamWriter(path, false, new UTF8Encoding(false));
        }

        public void WriteLine(string line)
        {
            _writer.Write(line);
            _writer.Write('\n');
            if (++_lines % 50 == 0) _writer.Flush();
        }

        public void Close() => _writer.Dispose();
    }
}
