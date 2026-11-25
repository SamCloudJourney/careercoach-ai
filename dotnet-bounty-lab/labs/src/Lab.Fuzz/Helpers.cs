using System.Buffers;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace Lab.Fuzz;

internal static class Segments
{
    private sealed class Seg : ReadOnlySequenceSegment<byte>
    {
        public Seg(ReadOnlyMemory<byte> m, long idx) { Memory = m; RunningIndex = idx; }
        public Seg Append(ReadOnlyMemory<byte> m) { var s = new Seg(m, RunningIndex + Memory.Length); Next = s; return s; }
    }

    public static ReadOnlySequence<byte> Split(byte[] data, int size)
    {
        if (data.Length <= size) return new ReadOnlySequence<byte>(data);
        var first = new Seg(data.AsMemory(0, size), 0);
        var last = first;
        for (var i = size; i < data.Length; i += size)
            last = last.Append(data.AsMemory(i, Math.Min(size, data.Length - i)));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
}

internal static class Archives
{
    public static byte[] Tar()
    {
        var ms = new MemoryStream();
        using (var w = new TarWriter(ms, TarEntryFormat.Pax, leaveOpen: true))
        {
            w.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "dir/"));
            w.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, "dir/a.txt") { DataStream = new MemoryStream("hello"u8.ToArray()) });
            w.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "dir/link") { LinkName = "a.txt" });
        }
        return ms.ToArray();
    }

    public static byte[] Zip()
    {
        var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var s = z.CreateEntry("dir/a.txt").Open()) s.Write("hello"u8);
            z.CreateEntry("dir/sub/");
        }
        return ms.ToArray();
    }

    public static byte[] Gzip(string text)
    {
        var ms = new MemoryStream();
        using (var g = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true)) g.Write(Encoding.UTF8.GetBytes(text));
        return ms.ToArray();
    }

    public static byte[] Brotli(string text)
    {
        var ms = new MemoryStream();
        using (var b = new BrotliStream(ms, CompressionLevel.Optimal, leaveOpen: true)) b.Write(Encoding.UTF8.GetBytes(text));
        return ms.ToArray();
    }

    /// <summary>
    /// Extracts into root/a/b/c/dest and asserts nothing was written outside dest and no
    /// link inside dest resolves outside it (CWE-22 / CWE-59 "zip-slip" oracle).
    /// </summary>
    public static void AssertContained(Action<string> extract)
    {
        var root = Path.Combine(Path.GetTempPath(), "labfuzz-" + Guid.NewGuid().ToString("N"));
        var dest = Path.Combine(root, "a", "b", "c", "dest");
        Directory.CreateDirectory(dest);
        try
        {
            try { extract(dest); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or FormatException or NotSupportedException or EndOfStreamException) { }

            var destFull = Path.GetFullPath(dest) + Path.DirectorySeparatorChar;
            foreach (var entry in Directory.EnumerateFileSystemEntries(root, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 }))
            {
                var full = Path.GetFullPath(entry);
                var isAncestor = destFull.StartsWith(full + Path.DirectorySeparatorChar, StringComparison.Ordinal);
                if (!isAncestor && !full.StartsWith(destFull, StringComparison.Ordinal))
                    throw new InvariantViolation($"extraction wrote outside destination: {full}");
                var info = new FileInfo(full);
                if (info.LinkTarget is { } target)
                {
                    var resolved = Path.GetFullPath(Path.IsPathRooted(target) ? target : Path.Combine(Path.GetDirectoryName(full)!, target));
                    if (!resolved.StartsWith(destFull, StringComparison.Ordinal) && resolved + Path.DirectorySeparatorChar != destFull)
                        throw new InvariantViolation($"extracted link escapes destination: {full} -> {target}");
                }
            }
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
}
