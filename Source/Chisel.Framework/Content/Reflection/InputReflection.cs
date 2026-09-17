using System;

namespace Chisel.Framework;

public struct InputReflection
{
    public string Name;
    public uint Index;

    public InputReflection()
    {
        Name = string.Empty;
        Index = 0;
    }
}
