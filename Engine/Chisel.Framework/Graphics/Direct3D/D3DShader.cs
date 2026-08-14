using Chisel.Resource;
using System;

namespace Chisel.Framework;

internal class D3DShader : Disposable, IShader
{
    public string Entry { get; }
    public ShaderStage Stage { get; }
    public ShaderReflection Reflection { get; }
    internal byte[] Bytecode { get; }

    public D3DShader(string entry, ShaderStage stage, ShaderReflection reflection, byte[] bytecode)
    {
        Entry = entry;
        Stage = stage;
        Reflection = reflection;
        Bytecode = bytecode;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {

        }
    }
}
