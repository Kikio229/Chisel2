using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;

namespace Chisel.Framework;

public static class PackFileWriter
{
    public static void Write(string outputPath, List<(string RelativePath, byte[] Data)> entries)
    {
        List<(string Name, byte[] Data)> lumps = new List<(string, byte[])>();

        foreach ((string r, byte[] d) in entries)
        {
            byte[] compressed = Compress(d);
            bool useCompressed = compressed.Length < d.Length;
            byte[] payload = BuildPayload(useCompressed, d, compressed);
            lumps.Add((r, payload));
        }

        File.WriteAllBytes(outputPath, LumpFile.Write(PackFile.Magic, PackFile.FormatVersion, lumps));
    }

    private static byte[] BuildPayload(bool compressed, byte[] raw, byte[] compressedData)
    {
        byte[] body = compressed ? compressedData : raw;
        byte[] payload = new byte[5 + body.Length];

        // for reading, we need to know
        payload[0] = compressed ? (byte)1 : (byte)0;
        BitConverter.GetBytes(raw.Length).CopyTo(payload, 1);
        body.CopyTo(payload, 5);

        return payload;
    }

    private static byte[] Compress(byte[] raw)
    {
        using MemoryStream ms = new MemoryStream();
        using (DeflateStream ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            ds.Write(raw, 0, raw.Length);
        }

        return ms.ToArray();
    }
}
