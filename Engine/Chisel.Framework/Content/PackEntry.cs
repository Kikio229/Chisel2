using System;

namespace Chisel.Framework;

public struct PackEntry
{
    public string Path;
    public long Offset;
    public long Length;
    public AssetType Type;

    public PackEntry()
    {
        Path = string.Empty;
        Offset = 0;
        Length = 0;
        Type = AssetType.None;
    }
}