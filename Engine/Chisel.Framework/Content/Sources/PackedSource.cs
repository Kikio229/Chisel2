using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Chisel.Framework;

public class PackedSource : Disposable, ISource
{
    private FileStream _stream;
    private Dictionary<string, (int Offset, int Length)> _directory;

    public PackedSource(string path)
    {
        _stream = File.OpenRead(path);
        using BinaryReader r = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
        _directory = LumpFile.ReadDirectory(r, out _, out _);
    }

    public bool Exists(string relativePath)
    {
        return _directory.ContainsKey(relativePath);
    }

    public Stream Open(string relativePath)
    {
        (int offset, int length) = _directory[relativePath];
        _stream.Seek(offset, SeekOrigin.Begin);

        using BinaryReader r = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }
    }
}
