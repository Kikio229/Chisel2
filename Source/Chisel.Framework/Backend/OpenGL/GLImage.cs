using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

public class GLImage : Disposable, IImage
{
    public uint Width { get; }
    public uint Height { get; }
    public uint MipLevels { get; }
    public uint SampleCount { get; }
    public ImageFormat Format { get; }
    public ImageUsage Usage { get; }

    internal uint Handle { get; }
    internal TextureTarget Target { get; }

    private readonly GL _gl;

    public unsafe GLImage(GL gl, uint width, uint height, uint mips, uint samples, ImageFormat format, ImageUsage usage)
    {
        _gl = gl;
        Handle = _gl.GenTexture();

        TextureTarget target = samples > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D;
        (InternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType) = GLUtilities.GetNativeImageFormat(format);
        Target = target;

        _gl.BindTexture(target, Handle);

        if (samples > 1)
        {
            _gl.TexImage2DMultisample(TextureTarget.Texture2DMultisample, samples, internalFormat, width, height, true);
        }
        else
        {
            _gl.TexImage2D(target, 0, internalFormat, width, height, 0, pixelFormat, pixelType, null);
            _gl.TexParameter(target, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(target, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        }

        Width = width;
        Height = height;
        MipLevels = mips;
        SampleCount = samples;
        Format = format;
        Usage = usage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteTexture(Handle);
        }
    }
}
