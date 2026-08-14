using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Chisel.Resource;

static class LumpFile
{
    public static byte[] Write(uint magic, byte version, List<(string Name, byte[] Data)> lumps)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8, true);

        w.Write(magic);
        w.Write(version);
        w.Write(lumps.Count);

        // We need a TOC, but we can't write it until we know where the data will actually go
        List<long> patchPositions = new List<long>();

        // We'll make the "placeholder" TOC here:
        foreach((string name, byte[] data) in lumps)
        {
            WriteString(w, name);
            patchPositions.Add(ms.Position);
            w.Write(0);
            w.Write(0);
        }

        // and now we'll write the actual data
        int[] offsets = new int[lumps.Count];

        for(int i = 0; i < lumps.Count; i ++)
        {
            offsets[i] = (int)ms.Position;
            w.Write(lumps[i].Data);
        }

        // Now, we'll go fill in that TOC
        for (int i = 0; i < lumps.Count; i++)
        {
            ms.Seek(patchPositions[i],SeekOrigin.Begin);
            w.Write(offsets[i]);
            w.Write(lumps[i].Data.Length);
        }

        return ms.ToArray();
    }

    public static Dictionary<string,(int Offset, int Length)> ReadDirectory(BinaryReader r, out uint magic, out byte version)
    {
        magic = r.ReadUInt32();
        version = r.ReadByte();
        int count = r.ReadInt32();

        // We dont want to load all of the data in memory at once,
        // we just want the virtual file directories.
        Dictionary<string, (int, int)> directory = new Dictionary<string, (int, int)>(count);

        for (int i = 0; i < count; i++)
        {
            string name = ReadString(r);
            int offset = r.ReadInt32();
            int length = r.ReadInt32();
            directory[name] = (offset, length);
        }

        return directory;
    }

    // My own helpers so that weird system shit doesnt mess with my format
    static void WriteString(BinaryWriter w, string s)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(s);
        w.Write(bytes.Length);
        w.Write(bytes);
    }
    static string ReadString(BinaryReader r)
    {
        int len = r.ReadInt32();
        return len == 0 ? string.Empty : Encoding.Unicode.GetString(r.ReadBytes(len));
    }
}
