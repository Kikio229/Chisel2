using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal static class GLUtilities
{
    public static (TextureMinFilter, TextureMagFilter) GetNativeFilterMode(SamplerFilterMode mode)
    {
        bool anisotropic = (mode & (SamplerFilterMode.Anisotropic4x | SamplerFilterMode.Anisotropic8x | SamplerFilterMode.Anisotropic16x)) != 0;
        bool bilinear = anisotropic || (mode & SamplerFilterMode.Bilinear) != 0;

        // Anisotropic doesnt exist here yet
        bool mipLinear = anisotropic || (mode & SamplerFilterMode.MipmapBilinear) != 0;
        bool mipNearest = !anisotropic && !mipLinear && (mode & SamplerFilterMode.MipmapNearest) != 0;

        TextureMagFilter mag = bilinear ? TextureMagFilter.Linear : TextureMagFilter.Nearest;
        TextureMinFilter min;

        if (mipLinear)
        {
            min = bilinear ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.NearestMipmapLinear;
        }
        else if (mipNearest)
        {
            min = bilinear ? TextureMinFilter.LinearMipmapNearest : TextureMinFilter.NearestMipmapNearest;
        }
        else
        {
            min = bilinear ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
        }

        return (min, mag);
    }

    public static GLEnum GetNativeWrapMode(SamplerWrapMode mode)
    {
        return mode switch
        {
            SamplerWrapMode.Repeat => GLEnum.Repeat,
            SamplerWrapMode.Clamp => GLEnum.ClampToEdge,
            SamplerWrapMode.Mirror => GLEnum.MirroredRepeat,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    public static (InternalFormat, PixelFormat, PixelType) GetNativeImageFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8UNorm => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            ImageFormat.R8G8UNorm => (InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte),
            ImageFormat.R8G8B8A8UNorm => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
            ImageFormat.R8G8B8A8UNormSrgb => (InternalFormat.Srgb8Alpha8, PixelFormat.Rgba, PixelType.UnsignedByte),
            ImageFormat.R16UNorm => (InternalFormat.R16, PixelFormat.Red, PixelType.UnsignedShort),
            ImageFormat.R16G16UNorm => (InternalFormat.RG16, PixelFormat.RG, PixelType.UnsignedShort),
            ImageFormat.R16G16B16A16UNorm => (InternalFormat.Rgba16, PixelFormat.Rgba, PixelType.UnsignedShort),
            ImageFormat.R16Float => (InternalFormat.R16f, PixelFormat.Red, PixelType.HalfFloat),
            ImageFormat.R16G16Float => (InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat),
            ImageFormat.R16G16B16A16Float => (InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat),
            ImageFormat.R32Float => (InternalFormat.R32f, PixelFormat.Red, PixelType.Float),
            ImageFormat.R32G32Float => (InternalFormat.RG32f, PixelFormat.RG, PixelType.Float),
            ImageFormat.R32G32B32Float => (InternalFormat.Rgb32f, PixelFormat.Rgb, PixelType.Float),
            ImageFormat.R32G32B32A32Float => (InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            ImageFormat.R32UInt => (InternalFormat.R32ui, PixelFormat.RedInteger, PixelType.UnsignedInt),
            ImageFormat.R32G32UInt => (InternalFormat.RG32ui, PixelFormat.RGInteger, PixelType.UnsignedInt),
            ImageFormat.R32G32B32UInt => (InternalFormat.Rgb32ui, PixelFormat.RgbInteger, PixelType.UnsignedInt),
            ImageFormat.R32G32B32A32UInt => (InternalFormat.Rgba32ui, PixelFormat.RgbaInteger, PixelType.UnsignedInt),
            ImageFormat.D16UNorm => (InternalFormat.DepthComponent16, PixelFormat.DepthComponent, PixelType.UnsignedShort),
            ImageFormat.D24UNormS8UInt => (InternalFormat.Depth24Stencil8, PixelFormat.DepthStencil, PixelType.UnsignedInt248),
            ImageFormat.D32Float => (InternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float),
            ImageFormat.D32FloatS8UInt => (InternalFormat.Depth32fStencil8, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
    public static BufferUsageARB GetNativeBufferUsage(BufferType type)
    {
        return type switch
        {
            BufferType.GpuOnly => BufferUsageARB.StaticDraw,
            BufferType.Upload => BufferUsageARB.DynamicDraw,
            BufferType.Readback => BufferUsageARB.StreamRead,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static BufferTargetARB GetNativeBufferTarget(BufferUsage usage)
    {
        if ((usage & BufferUsage.Index) != 0)
        {
            return BufferTargetARB.ElementArrayBuffer;
        }
        else if ((usage & BufferUsage.Vertex) != 0)
        {
            return BufferTargetARB.ArrayBuffer;
        }
        else if ((usage & BufferUsage.Constant) != 0)
        {
            return BufferTargetARB.UniformBuffer;
        }
        else if ((usage & BufferUsage.Storage) != 0)
        {
            return BufferTargetARB.ShaderStorageBuffer;
        }
        else if ((usage & BufferUsage.Indirect) != 0)
        {
            return BufferTargetARB.DrawIndirectBuffer;
        }
        else if ((usage & BufferUsage.CopySrc) != 0)
        {
            return BufferTargetARB.CopyReadBuffer;
        }
        else if ((usage & BufferUsage.CopyDst) != 0)
        {
            return BufferTargetARB.CopyWriteBuffer;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(usage));
        }
    }

    public static ShaderType GetNativeShaderStage(ShaderStage stage)
    {
        if ((stage & ShaderStage.Vertex) != 0)
        {
            return ShaderType.VertexShader;
        }
        else if ((stage & ShaderStage.Pixel) != 0)
        {
            return ShaderType.FragmentShader;
        }
        else if ((stage & ShaderStage.Compute) != 0)
        {
            return ShaderType.ComputeShader;
        }
        else 
        {
            throw new ArgumentOutOfRangeException(nameof(stage));
        }
    }

    public static int GetNativeComponentCount(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Float1 => 1,
            VertexFormat.Float2 => 2,
            VertexFormat.Float3 => 3,
            VertexFormat.Float4 => 4,
            _ => 1
        };
    }

    public static VertexAttribIType GetNativeIntegerType(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Int1 => VertexAttribIType.Int,
            VertexFormat.UInt1 => VertexAttribIType.UnsignedInt,
            VertexFormat.Byte1 => VertexAttribIType.UnsignedByte,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    public static bool GetNativeIsIntegerFormat(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Int1 => true,
            VertexFormat.UInt1 => true,
            VertexFormat.Byte1 => true,
            _ => false
        };
    }

    public static PrimitiveType GetNativeTopologyMode(GraphicsTopology topology)
    {
        switch (topology)
        {
            case GraphicsTopology.TriangleList:
                return PrimitiveType.Triangles;
            case GraphicsTopology.TriangleStrip:
                return PrimitiveType.TriangleStrip;
            case GraphicsTopology.LineList:
                return PrimitiveType.Lines;
            case GraphicsTopology.LineStrip:
                return PrimitiveType.LineStrip;
            case GraphicsTopology.PointList:
                return PrimitiveType.Points;
            default:
                throw new ArgumentOutOfRangeException(nameof(topology));
        }
    }

    public static (bool, DepthFunction) GetNativeDepthMode(GraphicsDepthMode mode)
    {
        switch (mode)
        {
            case GraphicsDepthMode.Disabled:
                return (false, DepthFunction.Always);
            case GraphicsDepthMode.Less:
                return (true, DepthFunction.Less);
            case GraphicsDepthMode.LessOrEqual:
                return (true, DepthFunction.Lequal);
            case GraphicsDepthMode.Equal:
                return (true, DepthFunction.Equal);
            case GraphicsDepthMode.Greater:
                return (true, DepthFunction.Greater);
            case GraphicsDepthMode.GreaterOrEqual:
                return (true, DepthFunction.Gequal);
            case GraphicsDepthMode.Always:
                return (true, DepthFunction.Always);
            case GraphicsDepthMode.Never:
                return (true, DepthFunction.Never);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public static (bool, BlendingFactor, BlendingFactor, BlendEquationModeEXT) GetNativeBlendMode(GraphicsBlendMode mode)
    {
        switch (mode)
        {
            case GraphicsBlendMode.Opaque:
                return (false, BlendingFactor.One, BlendingFactor.Zero, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Alpha:
                return (true, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Additive:
                return (true, BlendingFactor.SrcAlpha, BlendingFactor.One, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Multiply:
                return (true, BlendingFactor.DstColor, BlendingFactor.Zero, BlendEquationModeEXT.FuncAdd);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public static (bool, TriangleFace) GetNativeCullMode(GraphicsCullMode mode)
    {
        switch (mode)
        {
            case GraphicsCullMode.None:
                return (false, TriangleFace.Back);
            case GraphicsCullMode.Front:
                return (true, TriangleFace.Front);
            case GraphicsCullMode.Back:
                return (true, TriangleFace.Back);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public static PolygonMode GetNativeFillMode(GraphicsFillMode mode)
    {
        switch (mode)
        {
            case GraphicsFillMode.Solid:
                return PolygonMode.Fill;
            case GraphicsFillMode.Wireframe:
                return PolygonMode.Line;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
