using Chisel.Resource;
using System;

namespace Chisel.Framework;

public struct ShaderDescription
{
    public string Entry;
    public ShaderStage Stage;
    public ReadOnlyMemory<byte> Bytecode;
    public ShaderReflection Reflection;

    public ShaderDescription()
    {
        Entry = string.Empty;
        Stage = ShaderStage.None;
        Bytecode = ReadOnlyMemory<byte>.Empty;
        Reflection = null;
    }
}