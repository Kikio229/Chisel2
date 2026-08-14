using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
internal class GLImage : Disposable, IImage
{
    public uint Width { get; }
    public uint Height { get; }
    public uint MipLevels { get; }
    public ImageFormat Format { get; }
    public ImageUsage Usage { get; }
    internal uint Handle { get; }
    internal TextureTarget Target { get; }
    internal uint SampleCount { get; }

    GL gl;

    public GLImage(GL gl, uint handle, uint width, uint height, uint mips, ImageFormat format, ImageUsage usage, uint sampleCount, TextureTarget target)
    {
        this.gl = gl;
        Handle = handle;
        Width = width;
        Height = height;
        MipLevels = mips;
        Format = format;
        Usage = usage;
        SampleCount = sampleCount;
        Target = target;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteTexture(Handle);
        }
    }
}
