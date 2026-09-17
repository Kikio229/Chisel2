using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.SDL3;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

public partial class GLGraphicsDevice : Disposable, IGraphicsDevice
{
    public uint FrameIndex => FrameIndexInternal;
    public uint SampleCount => SampleCountInternal;
    public uint BufferCount => BufferCountInternal;
    public ImageFormat[] ColorFormats => ColorFormatsInternal;
    public ImageFormat? DepthStencilFormat => DepthStencilFormatInternal;
    public GraphicsBackend Backend => GraphicsBackend.OpenGL; // This will never change obv

    internal uint FrameIndexInternal { get; set; }
    internal uint SampleCountInternal { get; set; }
    internal uint BufferCountInternal { get; set; }
    internal ImageFormat[] ColorFormatsInternal { get; set; }
    internal ImageFormat? DepthStencilFormatInternal { get; set; }
    internal static readonly ImageFormat[] BackBufferColorFormats = { ImageFormat.R8G8B8A8UNorm };

    private readonly GL _gl;
    private readonly SDLGLContext _glContext;
    private GLGraphicsState _currentState; // To avoid duplicate state changes

    private uint _currentVao;
    private uint[] _boundTextureBySlot, _boundSamplerBySlot;
    private Dictionary<(uint bufferHandle, VertexLayoutDescription layout), uint> _vaoCache;
    private Dictionary<uint, uint> _vbufferSlots; 
    private bool _isDebug;

    private DebugProc? _debugCallback;
    private Rectangle _currentViewport;
    
    public unsafe GLGraphicsDevice(SDLGLContext context, bool debug)
    {
        _glContext = context;
        _gl = GL.GetApi(UtilLoadGLFunction);
        _isDebug = debug;

        FrameIndexInternal = 0;
        SampleCountInternal = 1;
        BufferCountInternal = 2;
        ColorFormatsInternal = BackBufferColorFormats;
        DepthStencilFormatInternal = ImageFormat.D24UNormS8UInt;

        _boundTextureBySlot = new uint[16];
        _boundSamplerBySlot = new uint[16];
        _vaoCache = new Dictionary<(uint bufferHandle, VertexLayoutDescription layout), uint>();
        _vbufferSlots = new Dictionary<uint, uint>();

        string version = _gl.GetStringS(StringName.Version);

        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException("Failed to load OpenGL functions - is a context current?");
        }

        if (_isDebug)
        {
            if (!UtilHasExtension("GL_KHR_debug"))
            {
                Logger.AppendWarn("GL debug output requested, but GL_KHR_debug is not supported by this driver.");
                return;
            }

            _debugCallback = UtilOnDebugMessage;

            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous);
            _gl.DebugMessageCallback(_debugCallback, null);
        }

        SDL.GLSetSwapInterval(Game.Instance!.Window.IsVsyncOn ? 1 : 0);
        _currentState = new GLGraphicsState(); // Default state
        GC.SuppressFinalize(_currentState); // GC was randomly gobbling it up
        Logger.AppendLog("GL", "Successfully initialized OpenGL " + version, ConsoleColor.DarkCyan, 1);
    }

    public void BeginFrame()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        UtilResetToBackBufferFormats();
    }

    public unsafe void EndFrame()
    {
        SDL.GLSwapWindow(Game.Instance!.Window.Handle);
    }

    public void BeginDrawing(IRenderTarget target)
    {
        if (target is not GLRenderTarget glTarget)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            UtilResetToBackBufferFormats();
            return;
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, glTarget.Handle);

        if (glTarget.ColorInternal!.Length > 0)
        {
            ColorFormatsInternal = Array.ConvertAll(glTarget.ColorInternal, c => c.Format);
            SampleCountInternal = glTarget.DepthStencilInternal!.SampleCount;
        }
        else
        {
            ColorFormatsInternal = Array.Empty<ImageFormat>();
            SampleCountInternal = 1;
        }

        DepthStencilFormatInternal = glTarget.DepthStencilInternal?.Format;
    }

    public void EndDrawing()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        UtilResetToBackBufferFormats();
    }

    public void Clear(Color clearColor)
    {
        Clear(clearColor, 1.0f, 0, GraphicsClearFlags.Color | GraphicsClearFlags.Depth);
    }

    public void Clear(Color clearColor, float clearDepth, int clearStencil, GraphicsClearFlags flags)
    {
        Vector4 color = clearColor.ToVector4Normalized();
        _gl.ClearColor(color.X, color.Y, color.Z, color.W);
        _gl.ClearDepth(clearDepth);
        _gl.ClearStencil(clearStencil);

        ClearBufferMask mask = ClearBufferMask.None;
        bool depthClear = flags.HasFlag(GraphicsClearFlags.Depth);

        if (flags.HasFlag(GraphicsClearFlags.Color))
        {
            mask |= ClearBufferMask.ColorBufferBit;
        }

        if (flags.HasFlag(GraphicsClearFlags.Stencil))
        {
            mask |= ClearBufferMask.StencilBufferBit;
        }

        if (depthClear)
        {
            _gl.DepthMask(true);
            mask |= ClearBufferMask.DepthBufferBit;
        }

        _gl.Clear(mask);

        if (depthClear)
        {
            _gl.DepthMask(_currentState.DepthWriteEnabled);
        }
    }

    public void Resize(int width, int height)
    {
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void Draw(uint vertexCount)
    {
        _gl.DrawArrays(_currentState.Topology, 0, (uint)vertexCount);
    }

    public void DrawIndexed(uint indexCount)
    {
        DrawIndexed(indexCount, 0, 0);
    }

    public unsafe void DrawIndexed(uint indexCount, uint startIndex, int baseVertex)
    {
        _gl.DrawElementsBaseVertex(_currentState.Topology, indexCount, DrawElementsType.UnsignedInt, (void*)(startIndex * sizeof(uint)), baseVertex);
    }

    public void DrawInstanced(uint vertexCount, uint instCount)
    {
        _gl.DrawArraysInstanced(_currentState.Topology, 0, vertexCount, instCount);
    }

    public void DrawIndexedInstanced(uint indexCount, uint instCount)
    {
        DrawIndexedInstanced(indexCount, instCount, 0, 0);
    }

    public unsafe void DrawIndexedInstanced(uint indexCount, uint instCount, uint startIndex, int baseVertex)
    {
        _gl.DrawElementsInstanced(_currentState.Topology, indexCount, DrawElementsType.UnsignedInt, (void*)(startIndex * sizeof(uint)), instCount);
    }

    public void DrawIndirect(IBuffer buffer, uint drawCount)
    {
        DrawIndirect(buffer, 0, drawCount, 0);
    }

    public unsafe void DrawIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot GL draw indirect using a Non-GL buffer!");
        }

        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, glBuffer.Handle);
        _gl.MultiDrawArraysIndirect(_currentState.Topology, (void*)offset, drawCount, (uint)stride);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, 0);
    }

    public void DrawIndexedIndirect(IBuffer buffer, uint drawCount)
    {
        DrawIndexedIndirect(buffer, 0, drawCount, 0);
    }

    public unsafe void DrawIndexedIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot GL draw indexed indirect using a Non-GL buffer!");
        }

        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, glBuffer.Handle);
        _gl.MultiDrawElementsIndirect(_currentState.Topology, DrawElementsType.UnsignedInt, (void*)offset, drawCount, (uint)stride);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, 0);
    }

    public void Dispatch(uint groupX, uint groupY, uint groupZ)
    {
        _gl.DispatchCompute(groupX, groupY, groupZ);
    }

    public void DispatchIndirect(IBuffer buffer)
    {
        DispatchIndirect(buffer, 0);
    }

    public void DispatchIndirect(IBuffer buffer, ulong offset)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot GL dispatch indirect using a Non-GL buffer!");
        }

        _gl.BindBuffer(BufferTargetARB.DispatchIndirectBuffer, glBuffer.Handle);
        _gl.DispatchComputeIndirect((nint)offset);
        _gl.BindBuffer(BufferTargetARB.DispatchIndirectBuffer, 0);
    }

    public void SetViewport(Vector2 position, Vector2 size)
    {
        _gl.Viewport((int)position.X, (int)position.Y, (uint)size.X, (uint)size.Y);
        _currentViewport = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
    }

    public void SetScissor(Vector2 position, Vector2 size)
    {
        _gl.Enable(EnableCap.ScissorTest);
        _gl.Scissor((int)position.X, (int)position.Y, (uint)size.X, (uint)size.Y);
    }

    public void SetScissorEnabled(bool enabled)
    {
        if (enabled)
        {
            _gl.Enable(EnableCap.ScissorTest);
        }
        else
        {
            _gl.Disable(EnableCap.ScissorTest);
        }
    }

    public void SetConstants<T>(in T value, uint slot)
        where T : unmanaged
    {
        throw new NotImplementedException("Not implemented in GL");
    }

    public unsafe void SetVertexLayout(VertexLayoutDescription layout, uint slot)
    {
        if (!_vbufferSlots.TryGetValue(slot, out uint bufferHandle))
        {
            throw new InvalidOperationException("No vertex buffer bound to slot " + slot + " before SetVertexLayout.");
        }

        var key = (bufferHandle, layout);

        if (_vaoCache.TryGetValue(key, out uint cachedVao))
        {
            _gl.BindVertexArray(cachedVao);
            return; // as are already configured on this VAO from when it was built
        }

        uint newVao = _gl.GenVertexArray();
        _gl.BindVertexArray(newVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, bufferHandle);

        foreach (VertexAttributeDescription a in layout.Attributes)
        {
            int count = GLUtilities.GetNativeComponentCount(a.Format);

            if (GLUtilities.GetNativeIsIntegerFormat(a.Format))
            {
                _gl.VertexAttribIPointer(a.Location, count, GLUtilities.GetNativeIntegerType(a.Format), (uint)layout.Stride, (void*)a.Offset);
            }
            else
            {
                _gl.VertexAttribPointer(a.Location, count, VertexAttribPointerType.Float, false, (uint)layout.Stride, (void*)a.Offset);
            }

            _gl.EnableVertexAttribArray(a.Location);
        }

        _vaoCache[key] = newVao;
        _currentVao = newVao;
    }

    public void BindVertexBuffer(IBuffer buffer, uint slot)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot bind Non-GL vertex buffer to GL device!");
        }

        _vbufferSlots[slot] = glBuffer.Handle;
    }

    public void BindIndexBuffer(IBuffer buffer)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot bind Non-GL index buffer to GL device!");
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, glBuffer.Handle);
    }

    public void BindConstantBuffer(IBuffer buffer, uint slot)
    {
        BindConstantBuffer(buffer, 0, (uint)buffer.Size, slot);
    }

    public void BindConstantBuffer(IBuffer buffer, ulong offset, uint size, uint slot)
    {
        if (buffer is not GLBuffer glBuffer) 
        { 
            throw new InvalidOperationException("Cannot bind Non-GL constant buffer to GL device!"); 
        }

        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, glBuffer.Handle, (nint)offset, (nuint)size);
    }


    public void BindStorageBuffer(IBuffer buffer)
    {
        if (buffer is not GLBuffer glBuffer)
        {
            throw new InvalidOperationException("Cannot bind Non-GL storage buffer to GL device!");
        }

        throw new NotImplementedException("TODO: Implement in GL");
    }

    public void BindImage(IImage image, uint slot)
    {
        if (image is not GLImage glImage)
        {
            throw new InvalidOperationException("Cannot bind Non-GL image to GL device!");
        }

        if (_boundTextureBySlot[slot] == glImage.Handle)
        {
            return;
        }

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)slot);
        _gl.BindTexture(glImage.Target, glImage.Handle);
        _boundTextureBySlot[slot] = glImage.Handle;
    }

    public void BindSampler(ISampler sampler, uint slot)
    {
        if (sampler is not GLSampler glSampler)
        {
            throw new InvalidOperationException("Cannot bind Non-GL sampler to GL device!");
        }

        _gl.BindSampler(slot, glSampler.Handle);
    }

    public void BindGraphicsState(IGraphicsState graphicsState)
    {
        if (graphicsState is not GLGraphicsState glState)
        {
            throw new InvalidOperationException("Cannot bind Non-GL graphics state to GL device!");
        }

        if (glState.Handle != _currentState.Handle)
        {
            _gl.UseProgram(glState.Handle);
        }

        // Depth
        if (glState.DepthTestEnabled && !_currentState.DepthTestEnabled)
        {
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(glState.DepthFunc);
        }
        else if (!glState.DepthTestEnabled && _currentState.DepthTestEnabled)
        {
            _gl.Disable(EnableCap.DepthTest);
        }

        // Depth write
        if (glState.DepthWriteEnabled != _currentState.DepthWriteEnabled)
        {
            _gl.DepthMask(glState.DepthWriteEnabled);
        }

        // Blend
        if (glState.BlendEnabled && !_currentState.BlendEnabled)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(glState.BlendSrcFactor, glState.BlendDstFactor);
            _gl.BlendEquation(glState.BlendEquation);
        }
        else if (!glState.BlendEnabled && _currentState.BlendEnabled)
        {
            _gl.Disable(EnableCap.Blend);
        }

        // Cull
        if (glState.CullEnabled && !_currentState.CullEnabled)
        {
            _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(glState.CullFace);
        }
        else if (!glState.CullEnabled && _currentState.CullEnabled)
        {
            _gl.Disable(EnableCap.CullFace);
        }

        // Polygon mode
        if (glState.FillMode != _currentState.FillMode)
        {
            _gl.PolygonMode(TriangleFace.FrontAndBack, glState.FillMode);
        }

        _currentState = glState;
    }

    public void BindComputeState(IComputeState computeState)
    {
        if (computeState is not GLComputeState glState)
        {
            throw new InvalidOperationException("Cannot bind Non-GL compute state to GL device!");
        }

        throw new NotImplementedException("TODO: Implement in GL");
    }

    public void BindMaterialTable(IMaterialTable materialTable)
    {
        if (materialTable is not GLMaterialTable glTable)
        {
            throw new InvalidOperationException("Cannot bind Non-GL material table to GL device!");
        }

        for (uint i = 0; i < glTable.TexturesInternal.Length; i++)
        {
            BindImage(glTable.TexturesInternal[i], i);
        }
    }

    public void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data)
    {
        UpdateBuffer(buffer, data, 0);
    }

    public unsafe void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset)
    {
        GLBuffer glBuffer = (GLBuffer)buffer;
        BufferTargetARB target = GLUtilities.GetNativeBufferTarget(glBuffer.Usage);

        _gl.BindBuffer(target, glBuffer.Handle);

        fixed (byte* ptr = data)
        {
            _gl.BufferSubData(target, (nint)offset, (nuint)data.Length, ptr);
        }
    }

    public void SuballocBuffer(ReadOnlySpan<byte> data, out IBuffer arena, out ulong offset)
    {
        throw new NotImplementedException("TODO: Implement in GL");
    }

    public void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst)
    {
        BufferCopyRegion region = new BufferCopyRegion()
        {
            Size = bufferDst.Size,
            SrcOffset = 0,
            DstOffset = 0,
        };

        CopyBuffer(bufferSrc, bufferDst, region);
    }

    public unsafe void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst, BufferCopyRegion region)
    {
        if (bufferSrc is not GLBuffer glSrc)
        {
            throw new InvalidOperationException("Cannot copy from Non-GL buffer to GL buffer!");
        }

        if (bufferDst is not GLBuffer glDst)
        {
            throw new InvalidOperationException("Cannot copy to Non-GL buffer from GL buffer!");
        }

        _gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, glDst.Handle);
        _gl.BufferData((GLEnum)BufferTargetARB.CopyWriteBuffer, (nuint)region.DstOffset, null, (GLEnum)GLUtilities.GetNativeBufferTarget(glSrc.Usage));

        _gl.BindBuffer(BufferTargetARB.CopyReadBuffer, glSrc.Handle);
        _gl.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer, (nint)region.SrcOffset, (nint)region.DstOffset, (nuint)region.Size);
    }

    public void CopyBufferToImage(IBuffer bufferSrc, IImage imageDst)
    {
        ImageBufferCopyRegion region = new ImageBufferCopyRegion()
        {
            Width = imageDst.Width,
            Height = imageDst.Height,
            DstOffsetX = 0,
            DstOffsetY = 0,
            ImgMipLevel = 0,
            BuffOffset = 0,
        };

        CopyBufferToImage(bufferSrc, imageDst, region);
    }

    public unsafe void CopyBufferToImage(IBuffer bufferSrc, IImage imageDst, ImageBufferCopyRegion region)
    {
        if (bufferSrc is not GLBuffer glSrc)
        {
            throw new InvalidOperationException("Cannot copy from Non-GL buffer to GL image!");
        }

        if (imageDst is not GLImage glDst)
        {
            throw new InvalidOperationException("Cannot copy to Non-GL image from GL buffer!");
        }

        (_, PixelFormat pixelFormat, PixelType pixelType) = GLUtilities.GetNativeImageFormat(glDst.Format);

        _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, glSrc.Handle);
        _gl.BindTexture(TextureTarget.Texture2D, glDst.Handle);
        _gl.TexSubImage2D(TextureTarget.Texture2D, (int)region.ImgMipLevel, region.DstOffsetX, region.DstOffsetY, region.Width, region.Height, pixelFormat, pixelType, null);
        _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
    }

    public void ResolveImage(IImage imageSrc, IImage imageDst)
    {
        if (imageSrc is not GLImage glSrc)
        {
            throw new InvalidOperationException("Cannot copy from Non-GL image to GL image!");
        }

        if (imageDst is not GLImage glDst)
        {
            throw new InvalidOperationException("Cannot copy to Non-GL image from GL image!");
        }

        uint readFbo = _gl.GenFramebuffer();
        uint drawFbo = _gl.GenFramebuffer();

        _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFbo);
        _gl.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, glSrc.Target, glSrc.Handle, 0);

        _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFbo);
        _gl.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, glDst.Target, glDst.Handle, 0);

        _gl.BlitFramebuffer(0, 0, (int)glSrc.Width, (int)glSrc.Height, 0, 0, (int)glDst.Width, (int)glDst.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.DeleteFramebuffer(readFbo);
        _gl.DeleteFramebuffer(drawFbo);
    }

    public void CopyImage(IImage imageSrc, IImage imageDst)
    {
        ImageCopyRegion region = new ImageCopyRegion()
        {
            Width = imageSrc.Width,
            Height = imageSrc.Height,
            SrcOffsetX = 0,
            SrcOffsetY = 0,
            SrcMipLevel = 0,
            DstOffsetX = 0,
            DstOffsetY = 0,
            DstMipLevel = 0,
        };

        CopyImage(imageSrc, imageDst, region);
    }

    public void CopyImage(IImage imageSrc, IImage imageDst, ImageCopyRegion region)
    {
        if (imageSrc is not GLImage glSrc)
        {
            throw new InvalidOperationException("Cannot copy from Non-GL image to GL image!");
        }

        if (imageDst is not GLImage glDst)
        {
            throw new InvalidOperationException("Cannot copy to Non-GL image from GL image!");
        }

        _gl.CopyImageSubData(glSrc.Handle, CopyImageSubDataTarget.Texture2D, (int)region.SrcMipLevel, region.SrcOffsetX, region.SrcOffsetY, 0, 
            glDst.Handle, CopyImageSubDataTarget.Texture2D, (int)region.DstMipLevel, region.DstOffsetX, region.DstOffsetY, 0, region.Width, region.Height, 1);
    }

    public void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst)
    {
        ImageBufferCopyRegion region = new ImageBufferCopyRegion()
        {
            Width = imageSrc.Width,
            Height = imageSrc.Height,
            DstOffsetX = 0,
            DstOffsetY = 0,
            ImgMipLevel = 0,
            BuffOffset = 0,
        };

        CopyImageToBuffer(imageSrc, bufferDst, region);
    }

    public unsafe void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst, ImageBufferCopyRegion region)
    {
        if (imageSrc is not GLImage glSrc)
        {
            throw new InvalidOperationException("Cannot copy from Non-GL image to GL buffer!");
        }

        if (bufferDst is not GLBuffer glDst)
        {
            throw new InvalidOperationException("Cannot copy to Non-GL buffer from GL image!");
        }

        (_, PixelFormat pixelFormat, PixelType pixelType) = GLUtilities.GetNativeImageFormat(glSrc.Format);
        _gl.BindBuffer(BufferTargetARB.PixelPackBuffer, glDst.Handle);
        _gl.BindTexture(TextureTarget.Texture2D, glSrc.Handle);
        _gl.GetTexImage(TextureTarget.Texture2D, (int)region.ImgMipLevel, pixelFormat, pixelType, (void*)region.BuffOffset);
        _gl.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);
    }

    public IBuffer CreateBuffer(BufferDescription bufferDesc)
    {
        const BufferUsage knownFlags = BufferUsage.Vertex | BufferUsage.Index | BufferUsage.Constant | BufferUsage.Storage | BufferUsage.Indirect | BufferUsage.CopySrc;

        if (bufferDesc.Size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferDesc), "Buffer size must be greater than zero!");
        }

        if ((bufferDesc.Usage & ~knownFlags) != 0 || (!Enum.IsDefined(bufferDesc.Type)))
        {
            throw new ArgumentOutOfRangeException(nameof(bufferDesc), bufferDesc.Usage, "Buffer usage is unknown or invalid!");
        }

        GLBuffer buffer = new GLBuffer(_gl, bufferDesc.Size, bufferDesc.Type, bufferDesc.Usage);
        return (IBuffer)buffer;
    }

    public IImage CreateImage(ImageDescription imageDesc)
    {
        const ImageUsage knownFlags = ImageUsage.Sampled | ImageUsage.Storage | ImageUsage.RenderTarget | ImageUsage.DepthStencil;

        if (imageDesc.Width == 0 || imageDesc.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageDesc), "Image dimensions must be greater than zero!");
        }

        if (imageDesc.MipLevels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageDesc), "Mipmap levels should be at least one!");
        }

        if ((imageDesc.Usage & ~knownFlags) != 0 || !Enum.IsDefined(imageDesc.Format))
        {
            throw new ArgumentOutOfRangeException("Image format is unknown or invalid!", nameof(imageDesc));
        }

        if (imageDesc.SampleCount > 1 && imageDesc.Usage.HasFlag(ImageUsage.Sampled))
        {
            throw new ArgumentException("Multisampled images cannot be sampled directly. Resolve to a single-sample image first!");
        }

        GLImage image = new GLImage(_gl, imageDesc.Width, imageDesc.Height, imageDesc.MipLevels, imageDesc.SampleCount, imageDesc.Format, imageDesc.Usage);
        return (IImage)image;
    }

    public ISampler CreateSampler(SamplerDescription samplerDesc)
    {
        const SamplerFilterMode knownFlags = SamplerFilterMode.Bilinear | SamplerFilterMode.MipmapNearest | SamplerFilterMode.MipmapBilinear
            | SamplerFilterMode.Anisotropic4x | SamplerFilterMode.Anisotropic8x | SamplerFilterMode.Anisotropic16x;

        if ((samplerDesc.FilterMode & ~knownFlags) != 0 || !Enum.IsDefined(samplerDesc.WrapMode))
        {
            throw new ArgumentOutOfRangeException("Filter mode is unknown or invalid!", nameof(samplerDesc));
        }

        GLSampler sampler = new GLSampler(_gl, samplerDesc.DetailBias, samplerDesc.FilterMode, samplerDesc.WrapMode);
        return (ISampler)sampler;
    }

    public IShader CreateShader(ShaderDescription shaderDesc)
    {
        if (string.IsNullOrWhiteSpace(shaderDesc.Entry) || shaderDesc.Bytecode.IsEmpty)
        {
            throw new ArgumentException("Shader cannot be missing data or empty!", nameof(shaderDesc));
        }

        if (!Enum.IsDefined(shaderDesc.Stage))
        {
            throw new ArgumentOutOfRangeException(nameof(shaderDesc), shaderDesc.Stage, "Shader stage is unknown or invalid!");
        }

        GLShader shader = new GLShader(_gl, shaderDesc.Entry, shaderDesc.Stage, shaderDesc.Reflection!.Value, shaderDesc.Bytecode.Span);
        return (IShader)shader;
    }

    public IRenderTarget CreateRenderTarget(RenderTargetDescription targetDesc)
    {
        if (targetDesc.Color == null)
        {
            throw new ArgumentException("Color attachments cannot be null!", nameof(targetDesc));
        }

        if (targetDesc.Color.Length == 0 && targetDesc.DepthStencil == null)
        {
            throw new ArgumentException("A render target must have at least one attachment!", nameof(targetDesc));
        }

        GLRenderTarget target = new GLRenderTarget(_gl, targetDesc.Color.Cast<GLImage>().ToArray(), (GLImage?)targetDesc.DepthStencil);
        return (IRenderTarget)target;
    }

    public IGraphicsState CreateGraphicsState(GraphicsStateDescription graphicsDesc)
    {
        if (graphicsDesc.VertexShader != null && graphicsDesc.VertexShader.Stage != ShaderStage.Vertex)
        {
            throw new ArgumentException("A vertex shader state requires it to be a vertex stage!");
        }

        if (graphicsDesc.PixelShader != null && graphicsDesc.PixelShader.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("A fragment shader state requires it to be a fragment stage!");
        }

        if ((graphicsDesc.VertexShader != null && graphicsDesc.VertexShader is not GLShader) || (graphicsDesc.PixelShader != null && graphicsDesc.PixelShader is not GLShader))
        {
            throw new ArgumentException("Shaders must be a GLShader created by this device!", nameof(graphicsDesc));
        }

        if (!Enum.IsDefined(graphicsDesc.Topology) || !Enum.IsDefined(graphicsDesc.DepthMode) || !Enum.IsDefined(graphicsDesc.BlendMode) ||
            !Enum.IsDefined(graphicsDesc.CullMode) || !Enum.IsDefined(graphicsDesc.FillMode))
        {
            throw new ArgumentException("Provided rasterizer settings are unknown or invalid!");
        }

        GLGraphicsState state = new GLGraphicsState(_gl, (GLShader?)graphicsDesc.VertexShader, (GLShader?)graphicsDesc.PixelShader,
            graphicsDesc.ColorFormats, graphicsDesc.DepthStencilFormat, graphicsDesc.Topology, graphicsDesc.DepthMode, graphicsDesc.BlendMode, graphicsDesc.CullMode,
            graphicsDesc.FillMode, graphicsDesc.VertexLayout, graphicsDesc.AllowDepthWrite, graphicsDesc.SampleCount == 0 ? 1u : graphicsDesc.SampleCount);
        return (IGraphicsState)state;
    }

    public IComputeState CreateComputeState(ComputeStateDescription computeDesc)
    {
        throw new NotImplementedException("TODO: Implement in GL");
    }

    public IMaterialTable CreateMaterialTable(MaterialTableDescription materialDesc)
    {
        GLImage[] glTextures = new GLImage[materialDesc.Textures!.Length];

        for (int i = 0; i < materialDesc.Textures!.Length; i++)
        {
            glTextures[i] = (GLImage)materialDesc.Textures[i];
        }

        return new GLMaterialTable(materialDesc.Textures.Cast<GLImage>().ToArray()); // .NET is big stinky and won't let you cast class arrays
    }

    public void GenerateMipmaps(IImage image, ReadOnlySpan<byte> baseLevelData)
    {
        if (image is not GLImage glImage)
        {
            throw new InvalidOperationException("Cannot generate GL mips for Non-GL image!");
        }

        if (glImage.MipLevels <= 1)
        {
            return;
        }

        _gl.BindTexture(glImage.Target, glImage.Handle);
        _gl.GenerateMipmap(glImage.Target);
    }

    protected override void Dispose(bool disposing)
    {
        SDL.GLDestroyContext(_glContext);
    }

#region GL Util

    // Windows and GL is kinda silly
#if WINDOWS
    [LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint UtilWinGetProcAddress(nint module, string name);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleA", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint UtilWinGetModuleHandleA(string name);
#endif

    private void UtilOnDebugMessage(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
    {
        if (severity == GLEnum.DebugSeverityNotification)
        {
            return;
        }

        string text = Marshal.PtrToStringUTF8(message, length);
        Logger.AppendLog("GL", text, ConsoleColor.DarkCyan, 1);
    }

    private bool UtilHasExtension(string name)
    {
        _gl.GetInteger(GLEnum.NumExtensions, out int count);

        for (uint i = 0; i < count; i++)
        {
            string extension = _gl.GetStringS(StringName.Extensions, i);

            if (string.Equals(extension, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void UtilResetToBackBufferFormats()
    {
        ColorFormatsInternal = BackBufferColorFormats;
        DepthStencilFormatInternal = null;
        SampleCountInternal = 1;
    }

    private unsafe nint UtilLoadGLFunction(string name)
    {
        nint address = (nint)SDL.GLGetProcAddress(name);

#if WINDOWS
        if (address == 0)
        {
            nint module = UtilWinGetModuleHandleA("opengl32.dll");
            address = UtilWinGetProcAddress(module, name);
        }
#endif

        return address;
    }

    #endregion
}
