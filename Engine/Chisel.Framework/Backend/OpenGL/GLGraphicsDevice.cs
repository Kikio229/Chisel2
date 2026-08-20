
using Chisel.Resource;
using Hexa.NET.SDL3;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

public class GLGraphicsDevice : Disposable, IGraphicsDevice
{
    // fuckin windows... I'm not, but windows is weird
#if WINDOWS
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    static extern nint GetModuleHandleA(string moduleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    static extern nint GetProcAddress(nint module, string procName);
#endif

    public GraphicsBackend Backend => GraphicsBackend.OpenGL46; 
    // this doesnt really matter on GL
    public uint FrameIndex => 0;
    public uint BufferingCount => 2;

    internal GL gl;
    internal SDLGLContext glCTX;
    internal GLGraphicsState currentState; // To avoid duplicate state changes
    DebugProc debugCallback;

    private Dictionary<(uint bufferHandle, VertexLayoutDescription layout), uint> vaoCache = new();
    private Dictionary<uint, uint> vertexBufferSlots = new Dictionary<uint, uint>();
    private uint currentVaoInUse;

    private uint[] boundTextureBySlot = new uint[16];
    private uint[] boundSamplerBySlot = new uint[16];

    private Rectangle currentViewport;

    private static readonly ImageFormat[] _backBufferColorFormats = { ImageFormat.R8G8B8A8UNorm };
    private ImageFormat[] _currentColorFormats = _backBufferColorFormats;
    private ImageFormat? _currentDepthStencilFormat;
    private uint _currentSampleCount = 1;

    public ImageFormat[] ColorFormats => _currentColorFormats;
    public ImageFormat? DepthStencilFormat => _currentDepthStencilFormat;
    public uint SampleCount => _currentSampleCount;
    private void ResetToBackBufferFormats()
    {
        _currentColorFormats = _backBufferColorFormats;
        _currentDepthStencilFormat = null;
        _currentSampleCount = 1;
    }

    static unsafe nint LoadGLFunction(string name)
    {
        nint address = (nint)SDL.GLGetProcAddress(name);

#if WINDOWS
        if (address == 0)
        {
            nint module = GetModuleHandleA("opengl32.dll");
            address = GetProcAddress(module, name);
        }
#endif

        return address;
    }

    public unsafe GLGraphicsDevice(SDLGLContext context, bool debug)
    {
        glCTX = context;
        gl = GL.GetApi(LoadGLFunction);

        string version = gl.GetStringS(StringName.Version);

        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException("Failed to load OpenGL functions - is a context current?");
        }

        Logger.AppendLog("GL", "Successfully initialized OpenGL " + version, ConsoleColor.DarkCyan, 1);

        if (debug)
        {
            InitDebug();
        }

        SDL.GLSetSwapInterval(Game.Instance!.Window.IsVsyncOn ? 1 : 0);

        // Default state
        currentState = new GLGraphicsState(gl, 0, new GraphicsStateDescription());
        GC.SuppressFinalize(currentState); // GC was randomly gobbling it up
    }

    unsafe void InitDebug()
    {
        if (!HasExtension("GL_KHR_debug"))
        {
            Logger.AppendWarn("GL debug output requested, but GL_KHR_debug is not supported by this driver.");
            return;
        }

        debugCallback = OnDebugMessage;

        gl.Enable(EnableCap.DebugOutput);
        gl.Enable(EnableCap.DebugOutputSynchronous);
        gl.DebugMessageCallback(debugCallback, null);
    }

    private void OnDebugMessage(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
    {
        if (severity == GLEnum.DebugSeverityNotification)
        {
            return;
        }

        string text = Marshal.PtrToStringUTF8(message, length);
        Logger.AppendLog("GL", text, ConsoleColor.DarkCyan, 1);
    }

    bool HasExtension(string name)
    {
        gl.GetInteger(GLEnum.NumExtensions, out int count);

        for (uint i = 0; i < count; i++)
        {
            string extension = gl.GetStringS(StringName.Extensions, i);

            if (string.Equals(extension, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public unsafe void BeginFrame()
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        ResetToBackBufferFormats();
    }

    public unsafe void EndFrame()
    {
        // Swap buffers
        SDL.GLSwapWindow(Game.Instance!.Window.Handle);
    }

    public void BeginDrawing(IRenderTarget target)
    {
        // I've decided to auto-size the viewport when doing this. Seems like a good idea.
        if (target is GLRenderTarget glTarget)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, glTarget.Handle);

            IImage sizeSource = glTarget.Color != null && glTarget.Color.Length > 0 ? glTarget.Color[0] : glTarget.DepthStencil;

            if (sizeSource != null)
            {
                gl.Viewport(0, 0, sizeSource.Width, sizeSource.Height);
            }

            _currentColorFormats = glTarget.Color is { Length: > 0 }
                ? Array.ConvertAll(glTarget.Color, c => c.Format)
                : Array.Empty<ImageFormat>();
            _currentDepthStencilFormat = glTarget.DepthStencil?.Format;
            _currentSampleCount = glTarget.Color is { Length: > 0 } ? ((GLImage)glTarget.Color[0]).SampleCount
                : (glTarget.DepthStencil is GLImage depthImg ? depthImg.SampleCount : 1);
        }
        else
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            ResetToBackBufferFormats();
        }
    }

    public void EndDrawing()
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        ResetToBackBufferFormats();
    }

    public void Draw(uint vtxCount)
    {
        gl.DrawArrays(currentState.Topology, 0, (uint)vtxCount);
    }

    public unsafe void DrawIndexed(uint idxCount)
    {
        gl.DrawElements(currentState.Topology, (uint)idxCount, DrawElementsType.UnsignedInt, null);
    }

    public unsafe void DrawIndexed(uint idxCount, uint startIndex, int baseVertex)
    {
        gl.DrawElementsBaseVertex(currentState.Topology, idxCount, DrawElementsType.UnsignedInt,
            (void*)(startIndex * sizeof(uint)), baseVertex);
    }

    public void DrawInstanced(uint vtxCount, uint instCount)
    {
        gl.DrawArraysInstanced(currentState.Topology, 0, vtxCount, instCount);
    }

    public void DrawIndexedInstanced(uint idxCount, uint instCount)
    {
        DrawIndexedInstanced(idxCount, instCount, 0, 0);
    }

    public void DrawIndexedInstanced(uint idxCount, uint instCount, uint startIndex, int baseVertex)
    {
        // I think the input sig is wrong for this
    }

    public void DrawIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {

    }

    public void DrawIndexedIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {

    }

    public void Dispatch(uint groupX, uint groupY, uint groupZ)
    {

    }

    public void DispatchIndirect(IBuffer buffer, ulong offset)
    {

    }

    public void Clear(Color clearColor)
    {
        Clear(clearColor, 1.0f, 0, GraphicsClearFlags.Color | GraphicsClearFlags.Depth);
    }

    public void Clear(Color clearColor, float clearDepth, int clearStencil, GraphicsClearFlags flags)
    {
        Vector4 cc = clearColor.ToVector4();
        gl.ClearColor(cc.X, cc.Y, cc.Z, cc.W);
        gl.ClearDepth(clearDepth);
        gl.ClearStencil(clearStencil);

        bool clearingDepth = flags.HasFlag(GraphicsClearFlags.Depth);

        if (clearingDepth)
        {
            gl.DepthMask(true);
        }

        ClearBufferMask mask = ClearBufferMask.None;
        if (flags.HasFlag(GraphicsClearFlags.Color)) mask |= ClearBufferMask.ColorBufferBit;
        if (clearingDepth) mask |= ClearBufferMask.DepthBufferBit;
        if (flags.HasFlag(GraphicsClearFlags.Stencil)) mask |= ClearBufferMask.StencilBufferBit;

        gl.Clear(mask);

        if (clearingDepth)
        {
            gl.DepthMask(currentState.DepthWriteEnabled);
        }
    }

    public void Resize(int w, int h)
    {
        // GL dont care
    }

    public void SetViewport(Vector2 position, Vector2 size)
    {
        gl.Viewport((int)position.X, (int)position.Y, (uint)size.X, (uint)size.Y);
        currentViewport = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
    }

    public void SetScissor(Vector2 position, Vector2 size)
    {
        gl.Enable(EnableCap.ScissorTest);
        gl.Scissor((int)position.X, (int)position.Y, (uint)size.X, (uint)size.Y);
    }

    public void SetScissorEnabled(bool enabled)
    {
        if (enabled) gl.Enable(EnableCap.ScissorTest);
        else gl.Disable(EnableCap.ScissorTest);
    }

    public void GenerateMipmaps(IImage image, ReadOnlySpan<byte> baseLevelData)
    {
        GLImage glImage = (GLImage)image;
        if (glImage.MipLevels <= 1) return;

        gl.BindTexture(glImage.Target, glImage.Handle);
        gl.GenerateMipmap(glImage.Target);
    }

    public void SetConstants<T>(in T value, uint slot) 
        where T : unmanaged
    {
        throw new NotImplementedException("Not implemented in GL");
    }

    public void BindVertexBuffer(IBuffer buffer, uint slot)
    {
        GLBuffer glBuffer = (GLBuffer)buffer;
        vertexBufferSlots[slot] = glBuffer.Handle;
    }

    public unsafe void SetVertexLayout(VertexLayoutDescription layout, uint slot)
    {
        if (!vertexBufferSlots.TryGetValue(slot, out uint bufferHandle))
        {
            throw new InvalidOperationException("No vertex buffer bound to slot " + slot + " before SetVertexLayout.");
        }

        var key = (bufferHandle, layout);

        if (vaoCache.TryGetValue(key, out uint cachedVao))
        {
            gl.BindVertexArray(cachedVao);
            return; // attributes are already configured on this VAO from when it was built
        }

        uint newVao = gl.GenVertexArray();
        gl.BindVertexArray(newVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, bufferHandle);

        foreach (VertexAttributeDescription attribute in layout.Attributes)
        {
            int count = GetComponentCount(attribute.Format);

            if (IsIntegerFormat(attribute.Format))
            {
                gl.VertexAttribIPointer(attribute.Location, count, GetIntegerType(attribute.Format), (uint)layout.Stride, (void*)attribute.Offset);
            }
            else
            {
                gl.VertexAttribPointer(attribute.Location, count, VertexAttribPointerType.Float, false, (uint)layout.Stride, (void*)attribute.Offset);
            }

            gl.EnableVertexAttribArray(attribute.Location);
        }

        vaoCache[key] = newVao;
        currentVaoInUse = newVao;
    }

    public void BindIndexBuffer(IBuffer buffer)
    {
        if (currentVaoInUse == 0)
        {
            throw new InvalidOperationException("BindIndexBuffer called before SetVertexLayout - there's no VAO bound yet to associate the index buffer with.");
        }

        GLBuffer glBuffer = (GLBuffer)buffer;
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, glBuffer.Handle);
    }

    public void BindConstantBuffer(IBuffer buffer, uint slot)
    {
        GLBuffer glBuffer = (GLBuffer)buffer;
        gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, glBuffer.Handle);
    }


    // GL is not magic yet
    public void BindConstantBuffer(IBuffer buffer, ulong offset, uint size, uint slot)
    {
        throw new NotImplementedException();
    }

    public (IBuffer arena, ulong offset) SuballocateBuffer(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public void BindStorageBuffer(IBuffer buffer)
    {

    }

    public void BindImage(IImage image, uint slot)
    {
        GLImage glImage = (GLImage)image;

        if (boundTextureBySlot[slot] == glImage.Handle)
        {
            return;
        }

        gl.ActiveTexture(TextureUnit.Texture0 + (int)slot);
        gl.BindTexture(TextureTarget.Texture2D, glImage.Handle);
        boundTextureBySlot[slot] = glImage.Handle;
    }

    public void BindSampler(ISampler sampler, uint slot)
    {
        GLSampler glSampler = (GLSampler)sampler;
        gl.BindSampler(slot, glSampler.Handle);
    }

    public void BindGraphicsState(IGraphicsState gfxState)
    {
        if(gfxState is not GLGraphicsState state)
        {
            Logger.AppendWarn("Cannot bind non-GL graphics state to GL device!");
            return;
        }

        if (state.ProgramHandle != currentState.ProgramHandle)
        {
            gl.UseProgram(state.ProgramHandle);
        }

        // Depth
        if (state.DepthTestEnabled && !currentState.DepthTestEnabled)
        {
            gl.Enable(EnableCap.DepthTest);
            gl.DepthFunc(state.DepthFunc);
        }
        else if(!state.DepthTestEnabled && currentState.DepthTestEnabled)
        {
            gl.Disable(EnableCap.DepthTest);
        }

        // Depth write
        if (state.DepthWriteEnabled != currentState.DepthWriteEnabled)
        {
            gl.DepthMask(state.DepthWriteEnabled);
        }

        // Blend
        if (state.BlendEnabled && !currentState.BlendEnabled)
        {
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(state.BlendSrcFactor, state.BlendDstFactor);
            gl.BlendEquation(state.BlendEquation);
        }
        else if (!state.BlendEnabled && currentState.BlendEnabled)
        {
            gl.Disable(EnableCap.Blend);
        }

        // Cull
        if (state.CullEnabled && !currentState.CullEnabled)
        {
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(state.CullFace);
        }
        else if (!state.CullEnabled && currentState.CullEnabled)
        {
            gl.Disable(EnableCap.CullFace);
        }

        // Polygon mode
        if (state.FillMode != currentState.FillMode)
        {
            gl.PolygonMode(TriangleFace.FrontAndBack, state.FillMode);
        }

        currentState = state;
    }

    public void BindComputeState(IComputeState cmpState)
    {

    }

    public unsafe void CopyBuffer(IBuffer bufSrc, IBuffer bufDst)
    {
        GLBuffer glSrc = (GLBuffer)bufSrc;
        GLBuffer glDst = (GLBuffer)bufDst;

        gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, glDst.Handle);
        gl.BufferData((GLEnum)BufferTargetARB.CopyWriteBuffer, (nuint)glSrc.Size, null, (GLEnum)TranslateBufferTarget(glSrc.Usage));

        gl.BindBuffer(BufferTargetARB.CopyReadBuffer, glSrc.Handle);
        gl.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer, 0, 0, (nuint)glSrc.Size);
    }

    public unsafe void CopyBuffer(IBuffer bufSrc, IBuffer bufDst, BufferCopyRegion region)
    {
        throw new NotImplementedException("stub!!!");
    }

    public unsafe void CopyBufferToImage(IBuffer bufSrc, IImage imgDst)
    {
        GLBuffer glBuffer = (GLBuffer)bufSrc;
        GLImage glImage = (GLImage)imgDst;

        (_, PixelFormat pixelFormat, PixelType pixelType) = TranslateImageFormat(glImage.Format);

        gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, glBuffer.Handle);
        gl.BindTexture(TextureTarget.Texture2D, glImage.Handle);
        gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, glImage.Width, glImage.Height, pixelFormat, pixelType, null);
        gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
    }
    public unsafe void CopyBufferToImage(IBuffer bufSrc, IImage imgDst, ImageBufferCopyRegion region)
    {
        GLBuffer glBuffer = (GLBuffer)bufSrc;
        GLImage glImage = (GLImage)imgDst;

        (_, PixelFormat pixelFormat, PixelType pixelType) = TranslateImageFormat(glImage.Format);

        gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, glBuffer.Handle);
        gl.BindTexture(TextureTarget.Texture2D, glImage.Handle);
        gl.TexSubImage2D(TextureTarget.Texture2D, (int)region.ImgMipLevel, region.DstOffsetX, region.DstOffsetY, region.Width, region.Height, pixelFormat, pixelType, null);
        gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
    }

    public void CopyImage(IImage imgSrc, IImage imgDst)
    {
        throw new NotImplementedException("TODO: Image copying is not implemented on either backend yet!");
    }

    public void CopyImage(IImage imgSrc, IImage imgDst, ImageCopyRegion region)
    {
        throw new NotImplementedException("TODO: Image copying is not implemented on either backend yet!");
    }

    public void CopyImageToBuffer(IImage imgSrc, IBuffer bufDst)
    {
        throw new NotImplementedException("TODO: Image copying to buffer is not implemented on either backend yet!");
    }

    public void CopyImageToBuffer(IImage imgSrc, IBuffer bufDst, ImageBufferCopyRegion region)
    {
        throw new NotImplementedException("TODO: Image copying to buffer is not implemented on either backend yet!");
    }

    public unsafe void ResolveImage(IImage src, IImage dst)
    {
        GLImage glSrc = (GLImage)src;
        GLImage glDst = (GLImage)dst;

        uint readFbo = gl.GenFramebuffer();
        uint drawFbo = gl.GenFramebuffer();

        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFbo);
        gl.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, glSrc.Target, glSrc.Handle, 0);

        gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFbo);
        gl.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, glDst.Target, glDst.Handle, 0);

        gl.BlitFramebuffer(0, 0, (int)glSrc.Width, (int)glSrc.Height, 0, 0, (int)glDst.Width, (int)glDst.Height,
                                 ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(readFbo);
        gl.DeleteFramebuffer(drawFbo);
    }

    public unsafe IBuffer CreateBuffer(BufferDescription bufDesc)
    {
        uint handle = gl.GenBuffer();
        BufferTargetARB target = TranslateBufferTarget(bufDesc.Usage);
        BufferUsageARB usageHint = TranslateUsageHint(bufDesc.Type);

        gl.BindBuffer(target, handle);
        gl.BufferData(target, (nuint)bufDesc.Size, null, usageHint);

        return new GLBuffer(gl, handle, bufDesc.Size, bufDesc.Type, bufDesc.Usage);
    }

    public unsafe IImage CreateImage(ImageDescription imgDesc)
    {
        if (imgDesc.SampleCount > 1 && imgDesc.Usage.HasFlag(ImageUsage.Sampled))
        {
            throw new ArgumentException("Multisampled images cannot be sampled directly. Resolve to a single-sample image first.");
        }

        uint handle = gl.GenTexture();
        TextureTarget target = imgDesc.SampleCount > 1 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D;

        gl.BindTexture(target, handle);

        (InternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType) = TranslateImageFormat(imgDesc.Format);

        if (imgDesc.SampleCount > 1)
        {
            gl.TexImage2DMultisample(TextureTarget.Texture2DMultisample, imgDesc.SampleCount, internalFormat,
                                                                   (uint)imgDesc.Width, (uint)imgDesc.Height, true);
        }
        else
        {
            gl.TexImage2D(target, 0, internalFormat, imgDesc.Width, imgDesc.Height, 0, pixelFormat, pixelType, null);
            gl.TexParameter(target, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            gl.TexParameter(target, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        }

        return new GLImage(gl, handle, imgDesc.Width, imgDesc.Height, imgDesc.MipLevels, imgDesc.Format, imgDesc.Usage, imgDesc.SampleCount, target);
    }

    public ISampler CreateSampler(SamplerDescription smpDesc)
    {
        uint handle = gl.GenSampler();

        (TextureMinFilter minFilter, TextureMagFilter magFilter) = TranslateFilterMode(smpDesc.FilterMode);
        GLEnum wrap = TranslateWrapMode(smpDesc.WrapMode);

        gl.SamplerParameter(handle, SamplerParameterI.MinFilter, (int)minFilter);
        gl.SamplerParameter(handle, SamplerParameterI.MagFilter, (int)magFilter);
        gl.SamplerParameter(handle, SamplerParameterI.WrapS, (int)wrap);
        gl.SamplerParameter(handle, SamplerParameterI.WrapT, (int)wrap);

        return new GLSampler(gl, handle, smpDesc.DetailBias, smpDesc.FilterMode, smpDesc.WrapMode);
    }

    public IShader CreateShader(ShaderDescription shdDesc)
    {
        ShaderType stage = TranslateShaderStage(shdDesc.Stage);
        uint handle = gl.CreateShader(stage);

        // GL shaders are just strings
        string source = System.Text.Encoding.UTF8.GetString(shdDesc.Bytecode.Span);
        gl.ShaderSource(handle,source);
        gl.CompileShader(handle);
        gl.GetShader(handle, ShaderParameterName.CompileStatus, out int compileStatus);

        if(compileStatus == 0)
        {
            string log = gl.GetShaderInfoLog(handle);
            gl.DeleteShader(handle);
            throw new InvalidOperationException("Failed to compile GL shader: " + log);
        }

        return new GLShader(gl,shdDesc.Entry,shdDesc.Stage,shdDesc.Reflection,handle);
    }

    public unsafe IRenderTarget CreateRenderTarget(RenderTargetDescription renDesc)
    {
        uint handle = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);

        if (renDesc.Color != null)
        {
            for (int i = 0; i < renDesc.Color.Length; i++)
            {
                GLImage colorImage = (GLImage)renDesc.Color[i];
                gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, colorImage.Target, colorImage.Handle, 0);
            }
        }

        if (renDesc.DepthStencil is GLImage depthImage)
        {
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, depthImage.Target, depthImage.Handle, 0);
        }

        GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

        if (status != GLEnum.FramebufferComplete)
        {
            gl.DeleteFramebuffer(handle);
            throw new InvalidOperationException("Framebuffer incomplete: " + status);
        }

        return new GLRenderTarget(gl, handle, renDesc.Color, renDesc.DepthStencil);
    }

    public IGraphicsState CreateGraphicsState(GraphicsStateDescription gfxDesc)
    {
        uint programHandle = gl.CreateProgram();

        if(gfxDesc.VertexShader is GLShader vert)
        {
            gl.AttachShader(programHandle, vert.Handle);
        }
        else
        {
            Logger.AppendError("Attempted to bind a GL program to a non GL vertex shader!");
        }

        if (gfxDesc.PixelShader is GLShader frag)
        {
            gl.AttachShader(programHandle, frag.Handle);
        }
        else
        {
            Logger.AppendError("Attempted to bind a GL program to a non GL pixel shader!");
        }

        gl.LinkProgram(programHandle);
        gl.GetProgram(programHandle,ProgramPropertyARB.LinkStatus, out int linkStatus);

        if(linkStatus == 0)
        {
            string log = gl.GetProgramInfoLog(programHandle);
            gl.DeleteProgram(programHandle);
            throw new InvalidOperationException("Failed to link GL program: " + log);
        }

        // Apparently GL 3.3 is weird, so we have to do whatever tf this is:
        gl.UseProgram(programHandle);
        BindReflectedSlots(programHandle, gfxDesc.VertexShader);
        BindReflectedSlots(programHandle, gfxDesc.PixelShader);
        gl.UseProgram(0);

        return new GLGraphicsState(gl,programHandle,gfxDesc);
    }
    void BindReflectedSlots(uint programHandle, IShader shader)
    {
        if (shader == null)
        {
            return;
        }

        ShaderReflection reflection = shader.Reflection;

        // We have to kinda hack the uniforms
        foreach (ConstantBufferReflection cbuffer in reflection.ConstantBuffers)
        {
            uint blockIndex = gl.GetUniformBlockIndex(programHandle, cbuffer.Name);

            // I think that's the error code anyway
            if (blockIndex != uint.MaxValue)
            {
                gl.UniformBlockBinding(programHandle, blockIndex, cbuffer.Slot);
            }
        }

        foreach (ResourceReflection sampler in reflection.Samplers)
        {
            string glName = sampler.CompiledName ?? sampler.Name;
            int location = gl.GetUniformLocation(programHandle, glName);

            if(Game.Instance?.Window.IsDebug ?? false)
            {
                Logger.AppendInfo($"Bound to named GL sampler: {sampler.CompiledName}");
            }

            if (location >= 0)
            {
                gl.Uniform1(location, (int)sampler.Slot);
            }
        }
    }

    public IComputeState CreateComputeState(ComputeStateDescription cmpDesc)
    {
        return null; // TODO
    }

    public unsafe void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset = 0)
    {
        GLBuffer glBuffer = (GLBuffer)buffer;
        BufferTargetARB target = TranslateBufferTarget(glBuffer.Usage);

        gl.BindBuffer(target, glBuffer.Handle);

        fixed (byte* ptr = data)
        {
            gl.BufferSubData(target, (nint)offset, (nuint)data.Length, ptr);
        }
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (Backend == GraphicsBackend.OpenGL46)
        {
            SDL.GLDestroyContext(glCTX);
        }
    }


    // HELPERS
    // vvvvvv

    static (TextureMinFilter, TextureMagFilter) TranslateFilterMode(SamplerFilterMode mode)
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

    static GLEnum TranslateWrapMode(SamplerWrapMode mode)
    {
        switch (mode)
        {
            case SamplerWrapMode.Repeat:
                return GLEnum.Repeat;
            case SamplerWrapMode.Clamp:
                return GLEnum.ClampToEdge;
            case SamplerWrapMode.Mirror:
                return GLEnum.MirroredRepeat;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    static (InternalFormat, PixelFormat, PixelType) TranslateImageFormat(ImageFormat format)
    {
        switch (format)
        {
            case ImageFormat.R8UNorm:
                return (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte);
            case ImageFormat.R8G8UNorm:
                return (InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte);
            case ImageFormat.R8G8B8A8UNorm:
                return (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte);
            case ImageFormat.R8G8B8A8UNormSrgb:
                return (InternalFormat.Srgb8Alpha8, PixelFormat.Rgba, PixelType.UnsignedByte);
            case ImageFormat.R16UNorm:
                return (InternalFormat.R16, PixelFormat.Red, PixelType.UnsignedShort);
            case ImageFormat.R16G16UNorm:
                return (InternalFormat.RG16, PixelFormat.RG, PixelType.UnsignedShort);
            case ImageFormat.R16G16B16A16UNorm:
                return (InternalFormat.Rgba16, PixelFormat.Rgba, PixelType.UnsignedShort);
            case ImageFormat.R16Float:
                return (InternalFormat.R16f, PixelFormat.Red, PixelType.HalfFloat);
            case ImageFormat.R16G16Float:
                return (InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat);
            case ImageFormat.R16G16B16A16Float:
                return (InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat);
            case ImageFormat.R32Float:
                return (InternalFormat.R32f, PixelFormat.Red, PixelType.Float);
            case ImageFormat.R32G32Float:
                return (InternalFormat.RG32f, PixelFormat.RG, PixelType.Float);
            case ImageFormat.R32G32B32Float:
                return (InternalFormat.Rgb32f, PixelFormat.Rgb, PixelType.Float);
            case ImageFormat.R32G32B32A32Float:
                return (InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float);
            case ImageFormat.R32UInt:
                return (InternalFormat.R32ui, PixelFormat.RedInteger, PixelType.UnsignedInt);
            case ImageFormat.R32G32UInt:
                return (InternalFormat.RG32ui, PixelFormat.RGInteger, PixelType.UnsignedInt);
            case ImageFormat.R32G32B32UInt:
                return (InternalFormat.Rgb32ui, PixelFormat.RgbInteger, PixelType.UnsignedInt);
            case ImageFormat.R32G32B32A32UInt:
                return (InternalFormat.Rgba32ui, PixelFormat.RgbaInteger, PixelType.UnsignedInt);
            case ImageFormat.D16UNorm:
                return (InternalFormat.DepthComponent16, PixelFormat.DepthComponent, PixelType.UnsignedShort);
            case ImageFormat.D24UNormS8UInt:
                return (InternalFormat.Depth24Stencil8, PixelFormat.DepthStencil, PixelType.UnsignedInt248);
            case ImageFormat.D32Float:
                return (InternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float);
            case ImageFormat.D32FloatS8UInt:
                return (InternalFormat.Depth32fStencil8, PixelFormat.DepthStencil, PixelType.Float32UnsignedInt248Rev);
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }
    static BufferUsageARB TranslateUsageHint(BufferType type)
    {
        switch (type)
        {
            case BufferType.GpuOnly:
                return BufferUsageARB.StaticDraw;
            case BufferType.Upload:
                return BufferUsageARB.DynamicDraw;
            case BufferType.Readback:
                return BufferUsageARB.StreamRead;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
    static BufferTargetARB TranslateBufferTarget(BufferUsage usage)
    {
        if ((usage & BufferUsage.Index) != 0)
        {
            return BufferTargetARB.ElementArrayBuffer;
        }
        if ((usage & BufferUsage.Vertex) != 0)
        {
            return BufferTargetARB.ArrayBuffer;
        }
        if ((usage & BufferUsage.Constant) != 0)
        {
            return BufferTargetARB.UniformBuffer;
        }
        if ((usage & BufferUsage.Storage) != 0)
        {
            return BufferTargetARB.ShaderStorageBuffer;
        }
        if ((usage & BufferUsage.Indirect) != 0)
        {
            return BufferTargetARB.DrawIndirectBuffer;
        }
        if ((usage & BufferUsage.CopySource) != 0)
        {
            return BufferTargetARB.CopyReadBuffer;
        }
        throw new ArgumentOutOfRangeException(nameof(usage));
    }
    static ShaderType TranslateShaderStage(ShaderStage stage)
    {
        if ((stage & ShaderStage.Vertex) != 0)
        {
            return ShaderType.VertexShader;
        }
        if ((stage & ShaderStage.Pixel) != 0)
        {
            return ShaderType.FragmentShader;
        }
        if ((stage & ShaderStage.Compute) != 0)
        {
            return ShaderType.ComputeShader;
        }
        throw new ArgumentOutOfRangeException(nameof(stage));
    }

    static bool IsIntegerFormat(VertexFormat format)
    {
        switch (format)
        {
            case VertexFormat.Int1:
            case VertexFormat.UInt1:
            case VertexFormat.Byte1:
                return true;
            default:
                return false;
        }
    }
    static int GetComponentCount(VertexFormat format)
    {
        switch (format)
        {
            case VertexFormat.Float2:
                return 2;
            case VertexFormat.Float3:
                return 3;
            case VertexFormat.Float4:
                return 4;
            default:
                return 1;
        }
    }
    static VertexAttribIType GetIntegerType(VertexFormat format)
    {
        switch (format)
        {
            case VertexFormat.Int1:
                return VertexAttribIType.Int;
            case VertexFormat.UInt1:
                return VertexAttribIType.UnsignedInt;
            case VertexFormat.Byte1:
                return VertexAttribIType.UnsignedByte;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }
}
