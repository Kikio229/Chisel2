using System;

namespace Chisel.Framework;

public struct ResourceReflection
{
    public string Name;
    public string CompiledName; // Note: DX doesnt touch this
    public uint Slot;

    public ResourceReflection()
    {
        Name = string.Empty;
        CompiledName = string.Empty;
        Slot = 0;
    }
}