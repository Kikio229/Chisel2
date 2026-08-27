using System;

namespace Chisel.Framework;

public struct ShaderEntry
{
    public string Entry;
    public string Technique;
    public int BytecodeOffset;
    public int BytecodeLength;
    public int ReflectionOffset;
    public int ReflectionLength;
    public GraphicsBackend Backend;
    public ShaderStage Stage;

    public ShaderEntry()
    {
        Entry = string.Empty;
        Technique = string.Empty;
        BytecodeOffset = 0;
        BytecodeLength = 0;
        ReflectionOffset = 0;
        ReflectionLength = 0;
        Backend = GraphicsBackend.Auto;
        Stage = ShaderStage.None;
    }
}
