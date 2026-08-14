using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Chisel.Resource;
public class PakFile
{
    public const uint Magic = 0x4350414B;
    public const int FormatVersion = 1;

    public List<PakEntry> Entries = new List<PakEntry>();
}
public struct PakEntry
{
    public string Path;
    public AssetType Type;
    public long Offset;
    public long Length;
}

public static class PakWriter
{
    public static void Write(string outputPath, List<(string RelativePath, byte[] Data)> entries)
    {
        List<(string Name, byte[] Data)> lumps = new List<(string, byte[])>();

        foreach ((string relativePath, byte[] data) in entries)
        {
            byte[] compressed = Compress(data);
            bool useCompressed = compressed.Length < data.Length;
            byte[] payload = BuildPayload(useCompressed, data, compressed);
            lumps.Add((relativePath, payload));
        }

        File.WriteAllBytes(outputPath, LumpFile.Write(PakFile.Magic, PakFile.FormatVersion, lumps));
    }

    static byte[] BuildPayload(bool compressed, byte[] raw, byte[] compressedData)
    {
        byte[] body = compressed ? compressedData : raw;
        byte[] payload = new byte[5 + body.Length];

        // for reading, we need to know
        payload[0] = compressed ? (byte)1 : (byte)0;
        BitConverter.GetBytes(raw.Length).CopyTo(payload,1);
        body.CopyTo(payload, 5);
        return payload;
    }
    static byte[] Compress(byte[] raw)
    {
        using MemoryStream ms = new MemoryStream();
        using (DeflateStream ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            ds.Write(raw, 0, raw.Length);
        }
        return ms.ToArray();
    }
}