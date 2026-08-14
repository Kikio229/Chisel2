using Chisel.Resource;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
internal class GLShader : Disposable, IShader
{
    public string Entry { get; }
    public ShaderStage Stage { get; }
    public ShaderReflection Reflection { get; }
    internal uint Handle { get; }

    GL gl;
    public GLShader(GL gl, string entry, ShaderStage stage, ShaderReflection reflection, uint handle)
    {
        this.gl = gl;
        Entry = entry;
        Stage = stage;
        Reflection = reflection;
        Handle = handle;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteShader(Handle);
        }
    }
}
