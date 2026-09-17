using System;
using System.Text;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLShader : Disposable, IShader
{
    public string Entry { get; }
    public ShaderStage Stage { get; }
    public ShaderReflection Reflection { get; }

    internal uint Handle { get; }

    private readonly GL _gl;

    public GLShader(GL gl, string entry, ShaderStage stage, ShaderReflection reflection, ReadOnlySpan<byte> bytecode)
    {
        _gl = gl;
        Handle = _gl.CreateShader(GLUtilities.GetNativeShaderStage(stage));

        // GL shaders are just strings
        string source = Encoding.UTF8.GetString(bytecode);
        _gl.ShaderSource(Handle, source);
        _gl.CompileShader(Handle);
        _gl.GetShader(Handle, ShaderParameterName.CompileStatus, out int compileStatus);

        if (compileStatus == 0)
        {
            string log = _gl.GetShaderInfoLog(Handle);
            _gl.DeleteShader(Handle);
            throw new InvalidOperationException("Failed to compile GL shader: " + log);
        }

        Entry = entry;
        Stage = stage;
        Reflection = reflection;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteShader(Handle);
        }
    }
}
