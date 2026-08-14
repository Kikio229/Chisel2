using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.IO;

namespace Chisel.Resource;
public interface IContentSource : IDisposable
{
    bool Exists(string relativePath);
    Stream Open(string relativePath);
}
public class LooseContentSource : IContentSource
{
    string root;

    public LooseContentSource(string root)
    {
        this.root = root;
    }

    public Stream Open(string relativePath)
    {
        string nativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return File.OpenRead(Path.Combine(root, nativePath));
    }

    public bool Exists(string relativePath)
    {
        string nativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(root, nativePath));
    }

    public void Dispose()
    {
        // not needed
    }
}
public class PackedContentSource : IContentSource
{
    FileStream pakStream;
    Dictionary<string, (int Offset, int Length)> directory;

    public PackedContentSource(string pakPath)
    {
        pakStream = File.OpenRead(pakPath);
        using BinaryReader r = new BinaryReader(pakStream, Encoding.UTF8, leaveOpen: true);
        directory = LumpFile.ReadDirectory(r, out _, out _);
    }

    public bool Exists(string relativePath)
    {
        return directory.ContainsKey(relativePath);
    }

    public Stream Open(string relativePath)
    {
        (int offset, int length) = directory[relativePath];
        pakStream.Seek(offset, SeekOrigin.Begin);

        using BinaryReader r = new BinaryReader(pakStream, Encoding.UTF8, leaveOpen: true);
        byte compressedFlag = r.ReadByte();
        int originalLength = r.ReadInt32();
        byte[] body = r.ReadBytes(length - 5);

        if (compressedFlag == 0)
        {
            return new MemoryStream(body);
        }

        MemoryStream output = new MemoryStream(originalLength);
        using (MemoryStream compressedStream = new MemoryStream(body))
        using (DeflateStream ds = new DeflateStream(compressedStream, CompressionMode.Decompress))
        {
            ds.CopyTo(output);
        }
        output.Position = 0;
        return output;
    }

    public void Dispose()
    {
        pakStream.Dispose();
    }
}
public class MergedContentSource : IContentSource
{
    IContentSource primary;
    IContentSource fallback;

    public MergedContentSource(IContentSource primary, IContentSource fallback)
    {
        this.primary = primary;
        this.fallback = fallback;
    }

    public bool Exists(string relativePath)
    {
        return primary.Exists(relativePath) || fallback.Exists(relativePath);
    }

    public Stream Open(string relativePath)
    {
        if (primary.Exists(relativePath))
        {
            return primary.Open(relativePath);
        }

        return fallback.Open(relativePath);
    }

    public void Dispose()
    {
        primary.Dispose();
        fallback.Dispose();
    }
}