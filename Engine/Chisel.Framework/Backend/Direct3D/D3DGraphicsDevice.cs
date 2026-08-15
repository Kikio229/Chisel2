using Chisel.Resource;
using Hexa.NET.SDL3;
using Microsoft.Xna.Framework; // TODO: Change the XNA namespace
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Win32;
using Vortice.Win32.Graphics.D3D12MemoryAllocator;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D12;
using Vortice.Win32.Graphics.Dxgi;
using Vortice.Win32.Graphics.Dxgi.Common;
using Vortice.Win32.Numerics;
using static Vortice.Win32.Graphics.D3D12MemoryAllocator.Apis;
using static Vortice.Win32.Graphics.Direct3D12.Apis;
using static Vortice.Win32.Graphics.Dxgi.Apis;

namespace Chisel.Framework;

public class D3DGraphicsDevice : Disposable, IGraphicsDevice
{
    public uint CurrentSampleCount => _sampleCount;
    public GraphicsBackend Backend => GraphicsBackend.Direct3D12;
    public ImageFormat[] CurrentColorFormats => _colorFormats;
    public ImageFormat? CurrentDepthStencilFormat => _depthStencilFormat;
    public uint CurrentFrameIndex => _frameIndex;
    public uint BufferingCount => _maxFramesInFlight;

    private uint _renderHeapSize, _frameCount, _sampleCount;
    private ulong _mainFenceValue, _uploadFenceValue;
    private ImageFormat[] _colorFormats;
    private static readonly ImageFormat[] _backBufferColorFormats = { ImageFormat.R8G8B8A8UNorm };
    private ImageFormat? _depthStencilFormat;
    private bool _isDebug;

    // General state
    private D3DGraphicsState? _boundGfxState;
    private D3DComputeState? _boundCmpState;
    private D3DDescriptorHeap _resourceHeap, _samplerHeap;
    private Dictionary<uint, D3DBuffer> _vertexSlots, _constantSlots;
    private Dictionary<uint, D3DImage> _imageSlots;
    private Dictionary<uint, D3DSampler> _samplerSlots;

    // Per-frame draw-target state. The current target being null means the actual backbuffer
    private uint _frameIndex, _swapWidth, _swapHeight;
    private int _resizeWidth, _resizeHeight;
    private Vector2 _lastVpPosition, _lastVpSize; // Last viewport settings
    private D3DRenderTarget? _currentTarget;
    private ResourceStates[] _backBufferStates;
    private bool _isScissorEnabled, _isPendingResize;

    // Apparently our dumb asses destroyed all the benefit of double buffering... par for the course lol
    private ulong[] _frameFenceValues;

    // I placed these here thinking we'd be using root constants...
    // Not sure if we should keep these, but in the interest of not unequivocally breaking something
    // I'll leave these here for now
    private const uint _rootConstants = 0;
    private const uint _rootConstantBuffers = 1;
    private const uint _rootShaderResources = 2;
    private const uint _rootUnorderedAccess = 3;
    private const uint _rootSamplers = 4;

    // Layout of the resource heap (_resourceHeap)
    // This tracks where things go when we draw, basically our dynamic memory layout for rendering
    private const uint _drawsPerFrameCap = 256;       // bump-allocator budget, PER frame-in-flight
    private const uint _samplerDrawsPerFrameCap = 64; // per frame-in-flight - halved from 128, see note below
    private const uint _cbvRangeSize = 16;
    private const uint _srvRangeSize = 16;
    private const uint _uavRangeSize = 16;
    private const uint _samplerRangeSize = 16;

    private const uint _maxFramesInFlight = 2;

    // Each region is now _maxFramesInFlight independent slices, one per back-buffer index - so a
    // frame still executing on the GPU can never have its bound descriptors overwritten by the
    // *next* frame's binds.
    private const uint _cbvFrameStride = _drawsPerFrameCap * _cbvRangeSize;
    private const uint _srvFrameStride = _drawsPerFrameCap * _srvRangeSize;
    private const uint _uavFrameStride = _drawsPerFrameCap * _uavRangeSize;
    private const uint _samplerFrameStride = _samplerDrawsPerFrameCap * _samplerRangeSize;

    private const uint _cbvRegionStart = 0;
    private const uint _srvRegionStart = _cbvRegionStart + _maxFramesInFlight * _cbvFrameStride;
    private const uint _uavRegionStart = _srvRegionStart + _maxFramesInFlight * _srvFrameStride;
    private const uint _resourceHeapCapacity = _uavRegionStart + _maxFramesInFlight * _uavFrameStride;
    private const uint _samplerHeapCapacity = _maxFramesInFlight * _samplerFrameStride; // 2 * 64 * 16 = 2048

    // NEW
    // Rather than realloc every time that a new constant buffer + bindings + frame set is found,
    // we can allocate a "megabuffer" with 4MB of memory that those cbuffers can write into initially
    // before needing to allocate their own buffers.
    // This means we will shell out some memory constantly to this "arena buffer", but it means that
    // smaller and simpler cbuffers can flood it quickly without allocating more memory every time they grow.
    private const ulong _cbufferArenaCapacity = 4 * 1024 * 1024; // 4MB max, might raise
    private D3DBuffer[] _cbufferArenas;                          // one per working frame, so 2 for double buffer
    private unsafe void*[] _cbufferArenaMapped;                  // persistently-mapped pointer per lane
    private ulong[] _cbufferArenaCursor;                         // current cursor into the magic buffers

    // And just in case 4mb isnt enough...
    private List<D3DBuffer>[] _cbufferOverflowBuffers;

    // Bump cursors, reset every BeginFrame (safe: EndFrame already blocks until the GPU has fully
    // finished the previous frame, so nothing can still be reading these slots).
    private uint _cbvBumpCursor, _srvBumpCursor, _uavBumpCursor, _samplerBumpCursor;

    // The block this draw's BindConstantBuffer/BindImage/BindSampler calls should write into
    // set by BindGraphicsState (or BindComputeState), consumed by the three Bind* methods below.
    private uint _currentCbvBase, _currentSrvBase, _currentUavBase, _currentSamplerBase;
    private bool _hasDescriptorBlock;

    private static long _liveImageCreates, _liveBufferCreates, _liveGraphicsStateCreates;

    private readonly CpuDescriptorHandle[] _singleRtvScratch = new CpuDescriptorHandle[1];

    // DXGI
    private ComPtr<IDXGIFactory7> _factory;
    private ComPtr<IDXGIAdapter4> _adapter;
    private ComPtr<IDXGISwapChain4> _swapChain;

    // D3D12 misc
    private ComPtr<ID3D12DescriptorHeap> _renderHeap;
    private ComPtr<ID3D12Resource>[] _backBuffers;
    private Allocator _allocator;

    // D3D12 main
    // Contains both a main and command list/allocator used only for synchronous
    // "upload and wait" work (e.g. buffer/texture copies) that can happen at content-load time,
    // well outside the BeginFrame/EndFrame window the main _mainCmdList is scoped to.
    // At the moment this currently blocks, but can probably be made async
    private ComPtr<ID3D12Device8> _device;
    private ComPtr<ID3D12CommandQueue> _cmdQueue;
    private ComPtr<ID3D12GraphicsCommandList6> _mainCmdList, _uploadCmdList;
    private ComPtr<ID3D12CommandAllocator>[] _mainCmdAllocs;
    private ComPtr<ID3D12CommandAllocator> _uploadCmdAlloc;
    private ComPtr<ID3D12Fence1> _mainFence, _uploadFence;
    private ComPtr<ID3D12InfoQueue> _mainInfoQueue;
    private ComPtr<IDXGIInfoQueue> _dxgiInfoQueue;
    private AutoResetEvent _mainFenceEvent, _uploadFenceEvent;
    private FeatureLevel _featLevel;

    public unsafe D3DGraphicsDevice(bool debug)
    {
        _frameCount = _maxFramesInFlight;
        _sampleCount = 1;
        _mainFenceValue = 0;
        _uploadFenceValue = 0;
        _frameFenceValues = new ulong[_frameCount];
        _mainFenceEvent = new AutoResetEvent(false);
        _uploadFenceEvent = new AutoResetEvent(false);
        _featLevel = FeatureLevel.Level_11_0;
        _isDebug = debug;

        InitDebug();
        InitFactory();
        InitAdapter();
        InitDevice();
        InitInfoQueue();
        InitCommandQueue();
        InitSwapChain();
        InitBackBuffer();
        InitCommandAllocator();
        InitCommandList();
        InitFence();
        InitMemoryAllocator();
        QueryGpuInfo();

        // magic magic magic magic....
        InitConstantBufferArenas();

        _swapWidth = (uint)Game.Instance!.Window.Resolution.W;
        _swapHeight = (uint)Game.Instance!.Window.Resolution.H;
        _vertexSlots = new Dictionary<uint, D3DBuffer>();
        _constantSlots = new Dictionary<uint, D3DBuffer>();
        _imageSlots = new Dictionary<uint, D3DImage>();
        _samplerSlots = new Dictionary<uint, D3DSampler>();
        _resourceHeap = new D3DDescriptorHeap((ID3D12Device*)_device.Get(), DescriptorHeapType.CbvSrvUav, _resourceHeapCapacity, shaderVisible: true);
        _samplerHeap = new D3DDescriptorHeap((ID3D12Device*)_device.Get(), DescriptorHeapType.Sampler, _samplerHeapCapacity, shaderVisible: true);

        _backBufferStates = new ResourceStates[_frameCount];
        for (uint i = 0; i < _frameCount; i++)
        {
            // The swapchain hands these back already in the Present state
            _backBufferStates[i] = ResourceStates.Present;
        }
    }

    // ...magic magic...
    private unsafe void InitConstantBufferArenas()
    {
        _cbufferArenas = new D3DBuffer[_maxFramesInFlight];
        _cbufferArenaMapped = new void*[_maxFramesInFlight];
        _cbufferArenaCursor = new ulong[_maxFramesInFlight];
        _cbufferOverflowBuffers = new List<D3DBuffer>[_maxFramesInFlight];

        for (int lane = 0; lane < _maxFramesInFlight; lane++)
        {
            _cbufferArenas[lane] = new D3DBuffer(_allocator, _cbufferArenaCapacity, BufferType.Upload, BufferUsage.Constant);
            _cbufferOverflowBuffers[lane] = new List<D3DBuffer>();

            void* mapped;
            _cbufferArenas[lane].Resource->Map(0, null, &mapped);
            _cbufferArenaMapped[lane] = mapped;
        }
    }

    public unsafe void BeginFrame()
    {
        _frameIndex = _swapChain.Get()->GetCurrentBackBufferIndex();

        ulong waitValue = _frameFenceValues[_frameIndex];
        if (_mainFence.Get()->GetCompletedValue() < waitValue)
        {
            _mainFence.Get()->SetEventOnCompletion(waitValue, (Handle)_mainFenceEvent.SafeWaitHandle.DangerousGetHandle());
            _mainFenceEvent.WaitOne();
        }

        _cbvBumpCursor = 0;
        _srvBumpCursor = 0;
        _uavBumpCursor = 0;
        _samplerBumpCursor = 0;
        _hasDescriptorBlock = false;

        // reset the buffer cursor
        _cbufferArenaCursor[_frameIndex] = 0;

        foreach (D3DBuffer overflow in _cbufferOverflowBuffers[_frameIndex])
        {
            overflow.Dispose();
        }
        _cbufferOverflowBuffers[_frameIndex].Clear();

        _mainCmdAllocs[_frameIndex].Get()->Reset();
        _mainCmdList.Get()->Reset(_mainCmdAllocs[_frameIndex].Get(), null);

        ID3D12DescriptorHeap** heaps = stackalloc ID3D12DescriptorHeap*[2] { _resourceHeap.Heap, _samplerHeap.Heap };
        _mainCmdList.Get()->SetDescriptorHeaps(2, heaps);

        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), _backBuffers[_frameIndex].Get(), ref _backBufferStates[_frameIndex], ResourceStates.RenderTarget);

        _currentTarget = null;
        BindDefaultBackBufferTarget();
    }

    public unsafe void EndFrame()
    {
        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), _backBuffers[_frameIndex].Get(), ref _backBufferStates[_frameIndex], ResourceStates.Present);
        _mainCmdList.Get()->Close();

        ID3D12CommandList* cmdList = (ID3D12CommandList*)_mainCmdList.Get();
        _cmdQueue.Get()->ExecuteCommandLists(1, &cmdList);

        _swapChain.Get()->Present(
            (Game.Instance!.Window.IsVsyncOn) ? 1u : 0,
            (Game.Instance!.Window.IsVsyncOn) ? PresentFlags.None : PresentFlags.AllowTearing
        );

        FlushD3DInfoQueue();

        _mainFenceValue++;
        _cmdQueue.Get()->Signal((ID3D12Fence*)_mainFence.Get(), _mainFenceValue);
        _frameFenceValues[_frameIndex] = _mainFenceValue;

        if (_isPendingResize)
        {
            ResizeInternal(_resizeWidth, _resizeHeight);
            _isPendingResize = false;
        }
    }

    public unsafe void BeginDrawing(IRenderTarget target)
    {
        if (target is not D3DRenderTarget rt)
        {
            Logger.AppendWarn("Cannot bind non-D3D render target to D3D device!");
            return;
        }

        _currentTarget = rt;

        _colorFormats = rt.ColorImages is { Length: > 0 }
            ? Array.ConvertAll(rt.ColorImages, c => c.Format)
            : Array.Empty<ImageFormat>();
        _depthStencilFormat = rt.DepthStencilImage?.Format;
        _sampleCount = rt.ColorImages is { Length: > 0 }
            ? rt.ColorImages[0].SampleCount
            : (rt.DepthStencilImage?.SampleCount ?? 1);

        if (rt.ColorImages != null)
        {
            foreach (D3DImage color in rt.ColorImages)
            {
                BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), color.Resource, ref color.State, ResourceStates.RenderTarget);
            }
        }

        if (rt.DepthStencilImage != null)
        {
            BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), rt.DepthStencilImage.Resource, ref rt.DepthStencilImage.State, ResourceStates.DepthWrite);
        }

        uint rtvCount = (uint)(rt.RtvHandles?.Length ?? 0);

        if (rt.RtvHandles is { Length: > 0 })
        {
            fixed (CpuDescriptorHandle* rtvs = rt.RtvHandles)
            {
                if (rt.DsvHandle.HasValue)
                {
                    CpuDescriptorHandle dsv = rt.DsvHandle.Value;
                    _mainCmdList.Get()->OMSetRenderTargets(rtvCount, rtvs, false, &dsv);
                }
                else
                {
                    _mainCmdList.Get()->OMSetRenderTargets(rtvCount, rtvs, false, null);
                }
            }
        }
        else if (rt.DsvHandle.HasValue)
        {
            CpuDescriptorHandle dsv = rt.DsvHandle.Value;
            _mainCmdList.Get()->OMSetRenderTargets(0, null, false, &dsv);
        }

        // Same convention as GL: auto-size the viewport/scissor from the target's own dimensions.
        // We don't have to do this, but I really dont see why you wouldnt want to.
        SetViewport(Vector2.Zero, new Vector2(rt.Width, rt.Height));
        SetScissor(Vector2.Zero, new Vector2(rt.Width, rt.Height));
    }

    public unsafe void EndDrawing()
    {
        if (_currentTarget?.ColorImages != null)
        {
            foreach (D3DImage color in _currentTarget.ColorImages)
            {
                // Only transition to a readable state if something could plausibly sample it later,
                // an attachment that's RenderTarget-only (e.g. an MSAA color target that only ever
                // gets resolved, never sampled directly) has no reason to leave RenderTarget state.
                if ((color.Usage & ImageUsage.Sampled) != 0)
                {
                    BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), color.Resource, ref color.State, ResourceStates.PixelShaderResource);
                }
            }
        }

        _currentTarget = null;
        BindDefaultBackBufferTarget();

        // EndDrawing does *not* restore whatever viewport/scissor was active before BeginDrawing.
        // Could probably add that, I dont think we really need to.
    }

    private unsafe CpuDescriptorHandle[] CurrentRtvHandles()
    {
        if (_currentTarget == null)
        {
            CpuDescriptorHandle rtv = _renderHeap.Get()->GetCPUDescriptorHandleForHeapStart();
            rtv.ptr += (nuint)(_frameIndex * _renderHeapSize);
            _singleRtvScratch[0] = rtv;
            return _singleRtvScratch;
        }

        return _currentTarget.RtvHandles ?? Array.Empty<CpuDescriptorHandle>();
    }

    private CpuDescriptorHandle? CurrentDsvHandle() => _currentTarget?.DsvHandle;

    // Shorthand for the flag version, this one just clears color and depth (if available)
    public unsafe void Clear(Color clearColor)
    {
        Vector4 cc = clearColor.ToVector4();
        float* rgba = stackalloc float[4] { cc.X, cc.Y, cc.Z, cc.W };

        foreach (CpuDescriptorHandle rtv in CurrentRtvHandles())
        {
            _mainCmdList.Get()->ClearRenderTargetView(rtv, rgba, 0, null);
        }

        CpuDescriptorHandle? dsv = CurrentDsvHandle();

        if (dsv.HasValue)
        {
            _mainCmdList.Get()->ClearDepthStencilView(dsv.Value, ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0, 0, null);
        }
    }

    public unsafe void Clear(GraphicsClearFlags flags, Color clearColor, float clearDepth, int clearStencil)
    {
        if (flags.HasFlag(GraphicsClearFlags.Color))
        {
            Vector4 cc = clearColor.ToVector4();
            float* rgba = stackalloc float[4] { cc.X, cc.Y, cc.Z, cc.W };

            foreach (CpuDescriptorHandle rtv in CurrentRtvHandles())
            {
                _mainCmdList.Get()->ClearRenderTargetView(rtv, rgba, 0, null);
            }
        }

        bool clearingDepth = flags.HasFlag(GraphicsClearFlags.Depth);
        bool clearingStencil = flags.HasFlag(GraphicsClearFlags.Stencil);
        CpuDescriptorHandle? dsv = CurrentDsvHandle();

        if (dsv.HasValue && (clearingDepth || clearingStencil))
        {
            ClearFlags dxFlags = 0;

            if (clearingDepth)
            {
                dxFlags |= ClearFlags.Depth;
            }

            if (clearingStencil)
            {
                dxFlags |= ClearFlags.Stencil;
            }

            _mainCmdList.Get()->ClearDepthStencilView(dsv.Value, dxFlags, clearDepth, (byte)clearStencil, 0, null);
        }
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _isPendingResize = true; // Enable this to enable resizing.
        _resizeWidth = width;
        _resizeHeight = height;
    }

    unsafe void ResizeInternal(int width, int height)
    {
        if ((uint)width == _swapWidth && (uint)height == _swapHeight)
        {
            return;
        }

        WaitForGPU();

        for (uint i = 0; i < _frameCount; i++)
        {
            _backBuffers[i].Dispose();
        }

        SwapChainDescription1 desc;
        _swapChain.Get()->GetDesc1(&desc);

        if (_swapChain.Get()->ResizeBuffers(_frameCount, (uint)width, (uint)height, desc.Format, desc.Flags) != HResult.Ok)
        {
            FlushD3DInfoQueue();
            FlushDXGIInfoQueue();
            throw new InvalidOperationException("Failed to resize D3D swap chain buffers!");
        }

        ID3D12Resource** backBuffers = stackalloc ID3D12Resource*[(int)_frameCount];

        for (uint i = 0; i < _frameCount; i++)
        {
            fixed (Guid* gptr = &ID3D12Resource.IID_ID3D12Resource)
            {
                if (_swapChain.Get()->GetBuffer(i, gptr, (void**)&backBuffers[i]) != HResult.Ok)
                {
                    throw new InvalidOperationException("Failed to re-acquire D3D back buffers after resize!");
                }
            }
        }

        RenderTargetViewDescription targDesc = new RenderTargetViewDescription()
        {
            Format = Format.R8G8B8A8Unorm,
            ViewDimension = RtvDimension.Texture2D
        };

        CpuDescriptorHandle start = _renderHeap.Get()->GetCPUDescriptorHandleForHeapStart();

        for (uint j = 0; j < _frameCount; j++)
        {
            CpuDescriptorHandle handle = start;
            handle.ptr += (nuint)(j * _renderHeapSize);
            _device.Get()->CreateRenderTargetView(backBuffers[j], &targDesc, handle);
        }

        // we are such morons
        for (int k = 0; k < _frameCount; k++)
        {
            _backBuffers[k].Attach(backBuffers[k]);
            _backBufferStates[k] = ResourceStates.Present;
        }

        _swapWidth = (uint)width;
        _swapHeight = (uint)height;
    }

    public unsafe void Draw(uint vtxCount)
    {
        if (_boundGfxState == null)
        {
            throw new InvalidOperationException("Cannot draw with no graphics state bound!");
        }

        _mainCmdList.Get()->DrawInstanced(vtxCount, 1, 0, 0);
    }

    public unsafe void DrawIndexed(uint idxCount)
    {
        if (_boundGfxState == null)
        {
            throw new InvalidOperationException("Cannot index draw with no graphics state bound!");
        }

        _mainCmdList.Get()->DrawIndexedInstanced(idxCount, 1, 0, 0, 0);
    }

    public unsafe void DrawIndexed(uint idxCount, uint startIndex, int baseVertex)
    {
        if (_boundGfxState == null)
        {
            throw new InvalidOperationException("Cannot index draw with no graphics state bound!");
        }

        _mainCmdList.Get()->DrawIndexedInstanced(idxCount, 1, startIndex, baseVertex, 0);
    }

    public unsafe void DrawInstanced(uint vtxCount, uint instCount)
    {
        if (_boundGfxState == null)
        {
            throw new InvalidOperationException("Cannot instance draw with no graphics state bound!");
        }

        _mainCmdList.Get()->DrawInstanced(vtxCount, instCount, 0, 0);
    }

    public unsafe void DrawIndexedInstanced(uint idxCount, uint instCount)
    {
        if (_boundGfxState == null)
        {
            throw new InvalidOperationException("DrawIndexedInstanced called with no graphics state bound.");
        }

        _mainCmdList.Get()->DrawIndexedInstanced(idxCount, instCount, 0, 0, 0);
    }

    public void DrawIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {
        throw new NotImplementedException("TODO: Indirect draws are not implemented on either backend yet!");
    }

    public void DrawIndexedIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride)
    {
        throw new NotImplementedException("TODO: Indirect draws are not implemented on either backend yet!");
    }

    public void Dispatch(uint groupX, uint groupY, uint groupZ)
    {
        throw new NotImplementedException("TODO: Compute is not implemented on either backend yet!");
    }

    public void DispatchIndirect(IBuffer buffer, ulong offset)
    {
        throw new NotImplementedException("TODO: Compute is not implemented on either backend yet!");
    }

    public unsafe void SetViewport(Vector2 position, Vector2 size)
    {
        _lastVpPosition = position;
        _lastVpSize = size;

        Viewport vp = new Viewport(position.X, position.Y, size.X, size.Y, 0.0f, 1.0f);
        _mainCmdList.Get()->RSSetViewports(1, &vp);

        // D3D12 has no "scissor disabled" state, so this just fills the screen.
        if (!_isScissorEnabled)
        {
            SetFullScissor();
        }
    }

    public unsafe void SetScissor(Vector2 position, Vector2 size)
    {
        _isScissorEnabled = true;
        Rect rect = new Rect((int)position.X, (int)position.Y, (int)(position.X + size.X), (int)(position.Y + size.Y));
        _mainCmdList.Get()->RSSetScissorRects(1, &rect);
    }

    public void SetScissorEnabled(bool enabled)
    {
        _isScissorEnabled = enabled;

        if (!enabled)
        {
            SetFullScissor();
        }
    }

    public void SetConstants<T>(in T value, uint slot)
        where T : unmanaged
    {
        throw new NotSupportedException("Push constants (SetConstants<T>) are not implemented on either backend.");
    }

    public unsafe void SetVertexLayout(VertexLayoutDescription layout, uint slot)
    {
        if (!_vertexSlots.TryGetValue(slot, out D3DBuffer d3dBuffer))
        {
            throw new InvalidOperationException("No vertex buffer bound to slot " + slot + " before SetVertexLayout.");
        }

        VertexBufferView view = new VertexBufferView
        {
            BufferLocation = d3dBuffer.Resource->GetGPUVirtualAddress(),
            SizeInBytes = (uint)d3dBuffer.Size,
            StrideInBytes = (uint)layout.Stride,
        };

        _mainCmdList.Get()->IASetVertexBuffers(slot, 1, &view);
    }

    public void BindVertexBuffer(IBuffer buffer, uint slot)
    {
        D3DBuffer d3dBuffer = (D3DBuffer)buffer;
        _vertexSlots[slot] = d3dBuffer;
    }

    public unsafe void BindIndexBuffer(IBuffer buffer)
    {
        D3DBuffer d3dBuffer = (D3DBuffer)buffer;

        IndexBufferView view = new IndexBufferView
        {
            BufferLocation = d3dBuffer.Resource->GetGPUVirtualAddress(),
            SizeInBytes = (uint)d3dBuffer.Size,
            Format = Format.R32Uint,
        };

        _mainCmdList.Get()->IASetIndexBuffer(&view);
    }

    public unsafe void BindConstantBuffer(IBuffer buffer, uint slot)
    {
        if (slot >= 16)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Constant buffer slots are limited to b0-b15 by the root signature!");
        }

        if (!_hasDescriptorBlock)
        {
            throw new InvalidOperationException("No graphics or compute state was bound -- There's no descriptor block to write to yet!");
        }

        D3DBuffer d3dBuffer = (D3DBuffer)buffer;
        _constantSlots[slot] = d3dBuffer;

        ConstantBufferViewDescription cbvDesc = new ConstantBufferViewDescription
        {
            BufferLocation = d3dBuffer.Resource->GetGPUVirtualAddress(),
            SizeInBytes = (uint)((d3dBuffer.Size + 255) & ~255ul),
        };

        _device.Get()->CreateConstantBufferView(&cbvDesc, _resourceHeap.GetCpuAt(_currentCbvBase + slot));
    }

    public unsafe void BindConstantBuffer(IBuffer buffer, ulong offset, uint size, uint slot)
    {
        if (slot >= 16) throw new ArgumentOutOfRangeException(nameof(slot), "...");
        if (!_hasDescriptorBlock) throw new InvalidOperationException("...");

        // 65536 is a hard D3D12 platform limit (4096 elements x 16 bytes)
        const uint maxCbvSize = 65536;
        uint alignedSize = (size + 255) & ~255u;

        if (alignedSize > maxCbvSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size),
                $"Requested constant buffer view size ({alignedSize} bytes) exceeds the D3D12 maximum of {maxCbvSize} bytes.");
        }

        D3DBuffer d3dBuffer = (D3DBuffer)buffer;

        ConstantBufferViewDescription cbvDesc = new ConstantBufferViewDescription
        {
            BufferLocation = d3dBuffer.Resource->GetGPUVirtualAddress() + offset,
            SizeInBytes = alignedSize, // the actual slice size, not the arena's total size
        };

        _device.Get()->CreateConstantBufferView(&cbvDesc, _resourceHeap.GetCpuAt(_currentCbvBase + slot));
    }

    public void BindStorageBuffer(IBuffer buffer)
    {
        throw new NotImplementedException("TODO: Storage buffers are not implemented on either backend yet!");
    }

    public unsafe void BindImage(IImage image, uint slot)
    {
        if (slot >= 16)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Image slots are limited to t0-t15 by the root signature.");
        }

        if (!_hasDescriptorBlock)
        {
            throw new InvalidOperationException("No graphics or compute state was bound -- There's no descriptor block to write to yet!");
        }

        D3DImage d3dImage = (D3DImage)image;
        _imageSlots[slot] = d3dImage;

        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), d3dImage.Resource, ref d3dImage.State, ResourceStates.PixelShaderResource);

        ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
        {
            Format = D3DUtilities.GetDxgiFormatFromImage(d3dImage.Format),
            ViewDimension = Vortice.Win32.Graphics.Direct3D12.SrvDimension.Texture2D,
            Shader4ComponentMapping = 5768,
            Anonymous = new ShaderResourceViewDescription._Anonymous_e__Union
            {
                Texture2D = new Texture2DSrv { MostDetailedMip = 0, MipLevels = d3dImage.MipLevels, PlaneSlice = 0, ResourceMinLODClamp = 0 }
            }
        };

        _device.Get()->CreateShaderResourceView(d3dImage.Resource, &srvDesc, _resourceHeap.GetCpuAt(_currentSrvBase + slot));
    }

    public unsafe void BindSampler(ISampler sampler, uint slot)
    {
        if (slot >= _samplerRangeSize)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Sampler slots are limited to s0-s15 by the root signature.");
        }

        if (!_hasDescriptorBlock)
        {
            throw new InvalidOperationException("No graphics or compute state was bound -- There's no descriptor block to write to yet!");
        }

        D3DSampler d3dSampler = (D3DSampler)sampler;
        _samplerSlots[slot] = d3dSampler;

        (Filter filter, float minLod, float maxLod) = GetFilterMode(d3dSampler.FilterMode);

        Vortice.Win32.Graphics.Direct3D12.SamplerDescription desc = new Vortice.Win32.Graphics.Direct3D12.SamplerDescription
        {
            Filter = filter,
            AddressU = GetWrapMode(d3dSampler.WrapMode),
            AddressV = GetWrapMode(d3dSampler.WrapMode),
            AddressW = GetWrapMode(d3dSampler.WrapMode),
            MipLODBias = d3dSampler.DetailBias,
            MaxAnisotropy = 1, // real anisotropic filtering isn't wired up yet
            ComparisonFunc = ComparisonFunction.Never,
            MinLOD = minLod,
            MaxLOD = maxLod,
        };

        _device.Get()->CreateSampler(&desc, _samplerHeap.GetCpuAt(_currentSamplerBase + slot));
    }

    public unsafe void BindGraphicsState(IGraphicsState gfxState)
    {
        if (gfxState is not D3DGraphicsState state)
        {
            throw new ArgumentException("Graphics state must be a D3DGraphicsState created by this device!", nameof(gfxState));
        }

        _boundGfxState = state;
        _boundCmpState = null;

        AllocDescriptorBlockForDraw();

        _mainCmdList.Get()->SetGraphicsRootSignature(state.RootSignature);
        _mainCmdList.Get()->SetPipelineState(state.PipelineState);
        _mainCmdList.Get()->IASetPrimitiveTopology(state.Topology);
        _mainCmdList.Get()->SetGraphicsRootDescriptorTable(_rootConstantBuffers, _resourceHeap.GetGpuAt(_currentCbvBase));
        _mainCmdList.Get()->SetGraphicsRootDescriptorTable(_rootShaderResources, _resourceHeap.GetGpuAt(_currentSrvBase));
        _mainCmdList.Get()->SetGraphicsRootDescriptorTable(_rootUnorderedAccess, _resourceHeap.GetGpuAt(_currentUavBase));
        _mainCmdList.Get()->SetGraphicsRootDescriptorTable(_rootSamplers, _samplerHeap.GetGpuAt(_currentSamplerBase));
    }

    public unsafe void BindComputeState(IComputeState cmpState)
    {
        if (cmpState is not D3DComputeState state)
        {
            throw new ArgumentException("Compute state must be a D3DComputeState created by this device!", nameof(cmpState));
        }

        _boundCmpState = state;
        _boundGfxState = null;

        AllocDescriptorBlockForDraw();

        _mainCmdList.Get()->SetComputeRootSignature(state.RootSignature);
        _mainCmdList.Get()->SetPipelineState(state.PipelineState);
        _mainCmdList.Get()->SetComputeRootDescriptorTable(_rootConstantBuffers, _resourceHeap.GetGpuAt(_currentCbvBase));
        _mainCmdList.Get()->SetComputeRootDescriptorTable(_rootShaderResources, _resourceHeap.GetGpuAt(_currentSrvBase));
        _mainCmdList.Get()->SetComputeRootDescriptorTable(_rootUnorderedAccess, _resourceHeap.GetGpuAt(_currentUavBase));
        _mainCmdList.Get()->SetComputeRootDescriptorTable(_rootSamplers, _samplerHeap.GetGpuAt(_currentSamplerBase));
    }

    public unsafe void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset)
    {
        D3DBuffer d3dBuffer = (D3DBuffer)buffer;

        if (d3dBuffer.Type != BufferType.Upload)
        {
            // GpuOnly/Readback buffers aren't CPU-mappable in D3D12. Nothing in this codebase
            // constructs one today is BufferType.Upload. A GpuOnly path would need a staging-buffer + CopyBuffer round trip.
            throw new NotSupportedException($"UpdateBuffer only supports {nameof(BufferType.Upload)} buffers on D3D12; got {d3dBuffer.Type}.");
        }

        void* mapped;

        if (d3dBuffer.Resource->Map(0, null, &mapped) != HResult.Ok)
        {
            HResult removedReason = _device.Get()->GetDeviceRemovedReason();
            FlushD3DInfoQueue();
            FlushDXGIInfoQueue();
            throw new InvalidOperationException(
                $"Failed to map D3D upload buffer! Device removed reason: {removedReason}");
        }

        Span<byte> dst = new Span<byte>((byte*)mapped + offset, data.Length);
        data.CopyTo(dst);

        d3dBuffer.Resource->Unmap(0, null);
    }

    public unsafe void CopyBuffer(IBuffer bufSrc, IBuffer bufDst)
    {
        D3DBuffer src = (D3DBuffer)bufSrc;
        D3DBuffer dst = (D3DBuffer)bufDst;

        if (dst.Type != BufferType.GpuOnly)
        {
            // Resources in D3D12 UPLOAD/READBACK heaps are required to stay in GenericRead/CopyDest
            // for their whole lifetime - they can't legally be a GPU copy destination. Use
            // UpdateBuffer (a CPU Map/memcpy) to write into an Upload buffer instead.
            throw new ArgumentException("Only a GpuOnly buffer can be the destination of CopyBuffer on D3D12.", nameof(bufDst));
        }

        ID3D12GraphicsCommandList* cmdList = BeginUpload();

        BarrierTransition(cmdList, dst.Resource, ref dst.State, ResourceStates.CopyDest);
        cmdList->CopyBufferRegion(dst.Resource, 0, src.Resource, 0, Math.Min(src.Size, dst.Size));
        BarrierTransition(cmdList, dst.Resource, ref dst.State, ResourceStates.Common);

        EndUploadAndWait();
    }

    public unsafe void CopyBufferToImage(IBuffer bufSrc, IImage imgDst)
    {
        D3DBuffer src = (D3DBuffer)bufSrc;
        D3DImage dst = (D3DImage)imgDst;

        ResourceDescription desc = dst.Resource->GetDesc();

        PlacedSubresourceFootprint footprint = default;
        ulong totalBytes;
        _device.Get()->GetCopyableFootprints(&desc, 0, 1, 0, &footprint, null, null, &totalBytes);

        // Texture2D.SetData hands CopyBufferToImage a *tightly packed* source buffer (width * bpp
        // per row, no padding) - that's the right, backend-agnostic thing for it to do, since GL's
        // TexSubImage2D doesn't care about row alignment. D3D12 does: CopyTextureRegion's source
        // footprint RowPitch must be a multiple of D3D12_TEXTURE_DATA_PITCH_ALIGNMENT (256), which
        // a tightly-packed row very often isn't. So we repack.
        uint tightRowPitch = dst.Width * D3DUtilities.GetBytesPerPixel(dst.Format);
        ulong paddedSize = footprint.Footprint.RowPitch * (ulong)footprint.Footprint.Height;

        D3DBuffer padded = new D3DBuffer(_allocator, paddedSize, BufferType.Upload, BufferUsage.CopySource);

        void* srcMapped;
        void* dstMapped;
        src.Resource->Map(0, null, &srcMapped);
        padded.Resource->Map(0, null, &dstMapped);

        for (uint row = 0; row < dst.Height; row++)
        {
            Buffer.MemoryCopy(
                (byte*)srcMapped + row * tightRowPitch,
                (byte*)dstMapped + row * footprint.Footprint.RowPitch,
                footprint.Footprint.RowPitch,
                tightRowPitch);
        }

        padded.Resource->Unmap(0, null);
        src.Resource->Unmap(0, null);

        ID3D12GraphicsCommandList* cmdList = BeginUpload();

        BarrierTransition(cmdList, dst.Resource, ref dst.State, ResourceStates.CopyDest);

        TextureCopyLocation dstLoc = new TextureCopyLocation
        {
            pResource = dst.Resource,
            Type = TextureCopyType.SubresourceIndex,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { SubresourceIndex = 0 }
        };

        TextureCopyLocation srcLoc = new TextureCopyLocation
        {
            pResource = padded.Resource,
            Type = TextureCopyType.PlacedFootprint,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { PlacedFootprint = footprint }
        };

        cmdList->CopyTextureRegion(&dstLoc, 0, 0, 0, &srcLoc, null);

        ResourceStates finalState = (dst.Usage & ImageUsage.Sampled) != 0 ? ResourceStates.PixelShaderResource : ResourceStates.Common;
        BarrierTransition(cmdList, dst.Resource, ref dst.State, finalState);

        EndUploadAndWait();
        padded.Dispose();
    }

    public unsafe void CopyBufferToImage(IBuffer bufSrc, IImage imgDst, ImageCopyRegion region)
    {
        D3DBuffer src = (D3DBuffer)bufSrc;
        D3DImage dst = (D3DImage)imgDst;

        ResourceDescription desc = dst.Resource->GetDesc();
        desc.Width = region.Width;
        desc.Height = region.Height;

        PlacedSubresourceFootprint footprint = default;
        ulong totalBytes;
        _device.Get()->GetCopyableFootprints(&desc, 0, 1, 0, &footprint, null, null, &totalBytes);

        uint tightRowPitch = region.Width * D3DUtilities.GetBytesPerPixel(dst.Format);
        ulong paddedSize = footprint.Footprint.RowPitch * (ulong)footprint.Footprint.Height;

        D3DBuffer padded = new D3DBuffer(_allocator, paddedSize, BufferType.Upload, BufferUsage.CopySource);

        void* srcMapped;
        void* dstMapped;
        src.Resource->Map(0, null, &srcMapped);
        padded.Resource->Map(0, null, &dstMapped);

        for (uint row = 0; row < region.Height; row++)
        {
            Buffer.MemoryCopy(
                (byte*)srcMapped + region.BufferOffset + row * tightRowPitch,
                (byte*)dstMapped + row * footprint.Footprint.RowPitch,
                footprint.Footprint.RowPitch,
                tightRowPitch);
        }

        padded.Resource->Unmap(0, null);
        src.Resource->Unmap(0, null);

        ID3D12GraphicsCommandList* cmdList = BeginUpload();

        BarrierTransition(cmdList, dst.Resource, ref dst.State, ResourceStates.CopyDest);

        TextureCopyLocation dstLoc = new TextureCopyLocation
        {
            pResource = dst.Resource,
            Type = TextureCopyType.SubresourceIndex,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { SubresourceIndex = region.MipLevel }
        };

        TextureCopyLocation srcLoc = new TextureCopyLocation
        {
            pResource = padded.Resource,
            Type = TextureCopyType.PlacedFootprint,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { PlacedFootprint = footprint }
        };

        cmdList->CopyTextureRegion(&dstLoc, (uint)region.DestX, (uint)region.DestY, 0, &srcLoc, null);

        ResourceStates finalState = (dst.Usage & ImageUsage.Sampled) != 0 ? ResourceStates.PixelShaderResource : ResourceStates.Common;
        BarrierTransition(cmdList, dst.Resource, ref dst.State, finalState);

        EndUploadAndWait();
        padded.Dispose();
    }

    public unsafe void ResolveImage(IImage src, IImage dst)
    {
        D3DImage d3dSrc = (D3DImage)src;
        D3DImage d3dDst = (D3DImage)dst;

        // Runs on the main command list, not the upload one.
        // ResolveImage is only ever called from RenderTarget2D.End(), which happens mid-frame (between BeginFrame/EndFrame),
        // same as Draw/Clear/BindGraphicsState above.
        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), d3dSrc.Resource, ref d3dSrc.State, ResourceStates.ResolveSource);
        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), d3dDst.Resource, ref d3dDst.State, ResourceStates.ResolveDest);

        _mainCmdList.Get()->ResolveSubresource(d3dDst.Resource, 0, d3dSrc.Resource, 0, D3DUtilities.GetDxgiFormatFromImage(d3dDst.Format));

        ResourceStates dstFinal = (d3dDst.Usage & ImageUsage.Sampled) != 0 ? ResourceStates.PixelShaderResource : ResourceStates.Common;
        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), d3dDst.Resource, ref d3dDst.State, dstFinal);
        BarrierTransition((ID3D12GraphicsCommandList*)_mainCmdList.Get(), d3dSrc.Resource, ref d3dSrc.State, ResourceStates.RenderTarget);
    }

    public void CopyImage(IImage imgSrc, IImage imgDst)
    {
        throw new NotImplementedException("TODO: Image copying is not implemented on either backend yet!");
    }

    public void CopyImageToBuffer(IImage imgSrc, IBuffer bufDst)
    {
        throw new NotImplementedException("TODO: Image copying to buffer is not implemented on either backend yet!");
    }

    // Because some TWAT at microsoft decided they didnt want hardware implementations anymore...
    // We get to do it ourselves.
    public void GenerateMips(IImage image, ReadOnlySpan<byte> baseLevelData)
    {
        D3DImage img = (D3DImage)image;
        if (img.MipLevels <= 1)
        {
            return;
        }

        if (img.SampleCount > 1)
        {
            throw new InvalidOperationException("Cannot generate mips for a multisampled image!");
        }

        if (!IsByteUNormFormat(img.Format))
        {
            Logger.AppendWarn($"Software mip generation currently only supports 8-bit-per-channel UNorm formats " +
                $"(R8UNorm/R8G8UNorm/R8G8B8A8UNorm/R8G8B8A8UNormSrgb), got {img.Format}.");

            return;
        }

        uint bpp = D3DUtilities.GetBytesPerPixel(img.Format);
        byte[] currentLevel = baseLevelData.ToArray();
        uint currentWidth = img.Width;
        uint currentHeight = img.Height;

        for (uint mip = 1; mip < img.MipLevels; mip++)
        {
            byte[] nextLevel = DownsampleBox2x2(currentLevel, currentWidth, currentHeight, bpp, out uint nextWidth, out uint nextHeight);
            UploadMipLevel(img, mip, nextLevel, nextWidth, nextHeight);

            currentLevel = nextLevel;
            currentWidth = nextWidth;
            currentHeight = nextHeight;
        }
    }

    public unsafe IBuffer CreateBuffer(BufferDescription bufDesc)
    {
        const BufferUsage knownFlags = BufferUsage.Vertex | BufferUsage.Index | BufferUsage.Constant | BufferUsage.Storage | BufferUsage.Indirect | BufferUsage.CopySource;

        if (bufDesc.Size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufDesc), "Buffer size must be greater than zero!");
        }

        if ((bufDesc.Usage & ~knownFlags) != 0 || (!Enum.IsDefined(bufDesc.Type)))
        {
            throw new ArgumentOutOfRangeException(nameof(bufDesc), bufDesc.Usage, "Buffer usage is unknown or invalid!");
        }

        ulong size = bufDesc.Size;

        if ((bufDesc.Usage & BufferUsage.Constant) != 0)
        {
            size = (size + 255) & ~255ul;
        }

        D3DBuffer buffer = new D3DBuffer(_allocator, size, bufDesc.Type, bufDesc.Usage);

        if (_isDebug)
        {
            Interlocked.Increment(ref _liveBufferCreates);
            Logger.AppendLog("D3D", $"CreateBuffer #{_liveBufferCreates}", ConsoleColor.Gray, 2);
        }

        return (IBuffer)buffer;
    }
    public unsafe (IBuffer arena, ulong offset) SuballocateConstantBuffer(ReadOnlySpan<byte> data)
    {
        uint lane = _frameIndex;
        ulong aligned = (_cbufferArenaCursor[lane] + 255) & ~255ul;

        if (aligned + (ulong)data.Length > _cbufferArenaCapacity)
        {
            // Arena exhausted for this frame-lane. Rather than fail the draw, fall back to a
            // one-off dedicated buffer. Write a warning though, because 4mb is kind of a lot
            // for simple data.
            Logger.AppendWarn(
                $"Constant buffer arena exhausted for frame lane {lane} (needed {data.Length} more " +
                $"bytes on top of {_cbufferArenaCursor[lane]} already used this frame, budget is " +
                $"{_cbufferArenaCapacity}). Falling back to a dedicated buffer for this bind - " +
                "consider raising _cbufferArenaCapacity if this happens often.");

            ulong paddedSize = ((ulong)data.Length + 255) & ~255ul;
            D3DBuffer fallback = new D3DBuffer(_allocator, paddedSize, BufferType.Upload, BufferUsage.Constant);
            UpdateBuffer(fallback, data, 0);
            _cbufferOverflowBuffers[lane].Add(fallback);

            return (fallback, 0);
        }

        data.CopyTo(new Span<byte>((byte*)_cbufferArenaMapped[lane] + aligned, data.Length));
        _cbufferArenaCursor[lane] = aligned + (ulong)data.Length;
        return (_cbufferArenas[lane], aligned);
    }

    public unsafe IImage CreateImage(ImageDescription imgDesc)
    {
        const ImageUsage knownFlags = ImageUsage.Sampled | ImageUsage.Storage | ImageUsage.RenderTarget | ImageUsage.DepthStencil;

        if (imgDesc.Width == 0 || imgDesc.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imgDesc), "Image dimensions must be greater than zero!");
        }

        if (imgDesc.MipLevels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imgDesc), "Mipmap levels should be at least one!");
        }

        if ((imgDesc.Usage & ~knownFlags) != 0 || !Enum.IsDefined(imgDesc.Format))
        {
            throw new ArgumentOutOfRangeException("Image format is unknown or invalid!", nameof(imgDesc));
        }

        if (imgDesc.SampleCount > 1 && imgDesc.Usage.HasFlag(ImageUsage.Sampled))
        {
            throw new ArgumentException("Multisampled images cannot be sampled directly. Resolve to a single-sample image first!");
        }

        D3DImage image = new D3DImage(_allocator, imgDesc.Width, imgDesc.Height, imgDesc.MipLevels, imgDesc.Format, imgDesc.Usage, imgDesc.SampleCount == 0 ? 1u : imgDesc.SampleCount);

        if (_isDebug)
        {
            Interlocked.Increment(ref _liveImageCreates);
            Logger.AppendLog("D3D", $"CreateImage #{_liveImageCreates} ({imgDesc.Width}x{imgDesc.Height})", ConsoleColor.Gray, 2);
        }

        return (IImage)image;
    }

    public ISampler CreateSampler(SamplerDescription smpDesc)
    {
        const SamplerFilterMode knownFlags = SamplerFilterMode.Bilinear | SamplerFilterMode.MipmapNearest | SamplerFilterMode.MipmapBilinear
            | SamplerFilterMode.Anisotropic4x | SamplerFilterMode.Anisotropic8x | SamplerFilterMode.Anisotropic16x;

        if ((smpDesc.FilterMode & ~knownFlags) != 0 || !Enum.IsDefined(smpDesc.WrapMode))
        {
            throw new ArgumentOutOfRangeException("Filter mode is unknown or invalid!", nameof(smpDesc));
        }

        D3DSampler sampler = new D3DSampler(smpDesc.DetailBias, smpDesc.FilterMode, smpDesc.WrapMode);
        return (ISampler)sampler;
    }

    public IShader CreateShader(ShaderDescription shdDesc)
    {
        if (string.IsNullOrWhiteSpace(shdDesc.Entry) || shdDesc.Bytecode.IsEmpty)
        {
            throw new ArgumentException("Shader cannot be missing data or empty!", nameof(shdDesc));
        }

        if (!Enum.IsDefined(shdDesc.Stage))
        {
            throw new ArgumentOutOfRangeException(nameof(shdDesc), shdDesc.Stage, "Shader stage is unknown or invalid!");
        }

        D3DShader shader = new D3DShader(shdDesc.Entry, shdDesc.Stage, shdDesc.Reflection, shdDesc.Bytecode.ToArray());
        return (IShader)shader;
    }

    public unsafe IRenderTarget CreateRenderTarget(RenderTargetDescription renDesc)
    {
        if (renDesc.Color == null)
        {
            throw new ArgumentException("Color attachments cannot be null!", nameof(renDesc));
        }

        if (renDesc.Color.Length == 0 && renDesc.DepthStencil == null)
        {
            throw new ArgumentException("A render target must have at least one attachment!", nameof(renDesc));
        }

        D3DImage[] colors = new D3DImage[renDesc.Color.Length];

        for (int i = 0; i < renDesc.Color.Length; i++)
        {
            if (renDesc.Color[i] is not D3DImage image)
            {
                throw new ArgumentException($"Color attachment {i} must be a D3DShader created by this device!", nameof(renDesc));
            }

            if ((image.Usage & ImageUsage.RenderTarget) == 0)
            {
                throw new ArgumentException($"Requested color attachment {i} does not support render-target usage!", nameof(renDesc));
            }

            colors[i] = image;
        }

        D3DImage? depthStencil = null;

        if (renDesc.DepthStencil != null)
        {
            if (renDesc.DepthStencil is not D3DImage image)
            {
                throw new ArgumentException("Depth-Stencil attachment must be a D3DShader created by this device!", nameof(renDesc));
            }

            if ((image.Usage & ImageUsage.DepthStencil) == 0)
            {
                throw new ArgumentException("Requested depth-stencil attachment does not support depth-stencil usage!", nameof(renDesc));
            }

            depthStencil = image;
        }

        uint width = colors.Length > 0 ? colors[0].Width : depthStencil!.Width;
        uint height = colors.Length > 0 ? colors[0].Height : depthStencil!.Height;

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i].Width != width || colors[i].Height != height)
            {
                throw new ArgumentException("All render-target attachments must have matching dimensions!", nameof(renDesc));
            }
        }

        if (depthStencil != null && (depthStencil.Width != width || depthStencil.Height != height))
        {
            throw new ArgumentException("All render-target attachments must have matching dimensions!", nameof(renDesc));
        }

        D3DRenderTarget target = new D3DRenderTarget((ID3D12Device*)_device.Get(), colors, depthStencil);
        return (IRenderTarget)target;
    }

    public unsafe IGraphicsState CreateGraphicsState(GraphicsStateDescription gfxDesc)
    {
        if (gfxDesc.VertexShader != null && gfxDesc.VertexShader.Stage != ShaderStage.Vertex)
        {
            throw new ArgumentException("A vertex shader state requires it to be a vertex stage!");
        }

        if (gfxDesc.PixelShader != null && gfxDesc.PixelShader.Stage != ShaderStage.Pixel)
        {
            throw new ArgumentException("A fragment shader state requires it to be a fragment stage!");
        }

        if ((gfxDesc.VertexShader != null && gfxDesc.VertexShader is not D3DShader) || (gfxDesc.PixelShader != null && gfxDesc.PixelShader is not D3DShader))
        {
            throw new ArgumentException("Shaders must be a D3DShader created by this device!", nameof(gfxDesc));
        }

        if (!Enum.IsDefined(gfxDesc.Topology) || !Enum.IsDefined(gfxDesc.DepthMode) || !Enum.IsDefined(gfxDesc.BlendMode) ||
            !Enum.IsDefined(gfxDesc.CullMode) || !Enum.IsDefined(gfxDesc.FillMode))
        {
            throw new ArgumentException("Provided rasterizer settings are unknown or invalid!");
        }

        D3DGraphicsState gfxState = new D3DGraphicsState((ID3D12Device*)_device.Get(), (D3DShader?)gfxDesc.VertexShader, (D3DShader?)gfxDesc.PixelShader,
                gfxDesc.ColorFormats, gfxDesc.DepthStencilFormat, gfxDesc.Topology, gfxDesc.DepthMode, gfxDesc.BlendMode, gfxDesc.CullMode,
                gfxDesc.FillMode, gfxDesc.VertexLayout, gfxDesc.AllowDepthWrite, gfxDesc.SampleCount == 0 ? 1u : gfxDesc.SampleCount);

        Interlocked.Increment(ref _liveGraphicsStateCreates);
        Logger.AppendLog("D3D", $"CreateGraphicsState #{_liveGraphicsStateCreates}", ConsoleColor.Yellow, 2);

        return (IGraphicsState)gfxState;
    }

    public unsafe IComputeState CreateComputeState(ComputeStateDescription cmpDesc)
    {
        if (cmpDesc.ComputeShader.Stage != ShaderStage.Compute)
        {
            throw new ArgumentException("A compute shader state requires it to be a compute stage!");
        }

        if (cmpDesc.ComputeShader is not D3DShader)
        {
            throw new ArgumentException("Shaders must be a D3DShader created by this device!", nameof(cmpDesc));
        }

        D3DComputeState cmpState = new D3DComputeState((ID3D12Device*)_device.Get(), (D3DShader)cmpDesc.ComputeShader);
        return (IComputeState)cmpState;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // just in case
            WaitForGPU();

            unsafe
            {
                if (_cbufferArenas != null)
                {
                    for (int lane = 0; lane < _cbufferArenas.Length; lane++)
                    {
                        _cbufferArenas[lane].Resource->Unmap(0, null);
                        _cbufferArenas[lane].Dispose();

                        foreach (D3DBuffer overflow in _cbufferOverflowBuffers[lane])
                        {
                            overflow.Dispose();
                        }
                    }
                }
            }
            foreach (var laneList in _cbufferOverflowBuffers)
            {
                foreach (var buf in laneList)
                {
                    buf.Dispose();
                }
            }

            _factory.Dispose();
            _adapter.Dispose();
            _swapChain.Dispose();
            _device.Dispose();
            _mainInfoQueue.Dispose();
            _dxgiInfoQueue.Dispose();
            _cmdQueue.Dispose();
            _mainCmdList.Dispose();
            foreach (var a in _mainCmdAllocs) a.Dispose();
            _renderHeap.Dispose();
            _resourceHeap.Dispose();
            _samplerHeap.Dispose();
            _uploadCmdList.Dispose();
            _uploadCmdAlloc.Dispose();
            _uploadFence.Dispose();

            foreach (var b in _backBuffers)
            {
                b.Dispose();
            }

            _mainFence.Dispose();
            _allocator.Release();
        }
    }

    #region D3D Util
    // Blocks until the GPU has caught up with everything submitted so far.
    // This drains the *main* queue, needed before touching
    // swap chain buffers since ResizeBuffers requires zero outstanding references to them.
    public unsafe void WaitForGPU()
    {
        _cmdQueue.Get()->Signal((ID3D12Fence*)_mainFence.Get(), _mainFenceValue);

        if (_mainFence.Get()->GetCompletedValue() < _mainFenceValue)
        {
            _mainFence.Get()->SetEventOnCompletion(_mainFenceValue, (Handle)_mainFenceEvent.SafeWaitHandle.DangerousGetHandle());
            _mainFenceEvent.WaitOne();
        }

        _mainFenceValue++;
    }

    private unsafe void FlushD3DInfoQueue()
    {
        if (_isDebug)
        {
            ulong count = _mainInfoQueue.Get()->GetNumStoredMessages();

            if (count > 0)
            {
                for (ulong i = 0; i < count; i++)
                {
                    nuint size = 0;
                    _mainInfoQueue.Get()->GetMessage(i, null, &size);

                    if (size == 0)
                    {
                        continue;
                    }

                    Message* message = (Message*)Marshal.AllocHGlobal((int)size);
                    _mainInfoQueue.Get()->GetMessage(i, message, &size);
                    Logger.AppendLog("D3D", Marshal.PtrToStringAnsi((nint)message->pDescription)!, ConsoleColor.DarkCyan, 1);
                    Marshal.FreeHGlobal((nint)message);
                }

                _mainInfoQueue.Get()->ClearStoredMessages();
            }
        }
    }

    private unsafe void FlushDXGIInfoQueue()
    {
        if (_isDebug)
        {
            ulong count = _dxgiInfoQueue.Get()->GetNumStoredMessages(DXGI_DEBUG_DXGI);

            if (count > 0)
            {
                for (ulong i = 0; i < count; i++)
                {
                    nuint size = 0;
                    _dxgiInfoQueue.Get()->GetMessage(DXGI_DEBUG_DXGI, i, null, &size);

                    if (size == 0)
                    {
                        continue;
                    }

                    InfoQueueMessage* message = (InfoQueueMessage*)Marshal.AllocHGlobal((int)size);
                    _dxgiInfoQueue.Get()->GetMessage(DXGI_DEBUG_DXGI, i, message, &size);
                    Logger.AppendLog("D3D", Marshal.PtrToStringAnsi((nint)message->pDescription)!, ConsoleColor.DarkCyan, 1);
                    Marshal.FreeHGlobal((nint)message);
                }

                _dxgiInfoQueue.Get()->ClearStoredMessages(DXGI_DEBUG_DXGI);
            }
        }
    }

    // Opens the upload command list for recording. Pairs with EndUploadAndWait().
    private unsafe ID3D12GraphicsCommandList* BeginUpload()
    {
        _uploadCmdAlloc.Get()->Reset();
        _uploadCmdList.Get()->Reset(_uploadCmdAlloc.Get(), null);
        return (ID3D12GraphicsCommandList*)_uploadCmdList.Get();
    }

    // Closes, executes, and blocks until the GPU has finished.
    // Simple and correct regardless of whether this runs at content-load time or mid-frame; not fast.
    private unsafe void EndUploadAndWait()
    {
        _uploadCmdList.Get()->Close();

        ID3D12CommandList* list = (ID3D12CommandList*)_uploadCmdList.Get();
        _cmdQueue.Get()->ExecuteCommandLists(1, &list);

        _uploadFenceValue++;
        _cmdQueue.Get()->Signal((ID3D12Fence*)_uploadFence.Get(), _uploadFenceValue);

        if (_uploadFence.Get()->GetCompletedValue() < _uploadFenceValue)
        {
            _uploadFence.Get()->SetEventOnCompletion(_uploadFenceValue, (Handle)_uploadFenceEvent.SafeWaitHandle.DangerousGetHandle());
            _uploadFenceEvent.WaitOne();
        }
    }

    private unsafe void BindDefaultBackBufferTarget()
    {
        CpuDescriptorHandle rtv = _renderHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        rtv.ptr += (nuint)(_frameIndex * _renderHeapSize);
        _mainCmdList.Get()->OMSetRenderTargets(1, &rtv, false, null);

        _colorFormats = _backBufferColorFormats;
        _depthStencilFormat = null;
        _sampleCount = 1;

        // D3D12 doesn't retain viewport/scissor across a command list Reset() the way GL's context
        // does. Re-apply every frame so the backbuffer always matches the *current* swap chain size.
        Vector2 size = new Vector2(_swapWidth, _swapHeight);
        SetViewport(Vector2.Zero, size);
        SetScissor(Vector2.Zero, size);
    }

    private unsafe void SetFullScissor()
    {
        Rect rect = new Rect(
            (int)_lastVpPosition.X,
            (int)_lastVpPosition.Y,
            (int)(_lastVpPosition.X + _lastVpSize.X),
            (int)(_lastVpPosition.Y + _lastVpSize.Y));

        _mainCmdList.Get()->RSSetScissorRects(1, &rect);
    }

    private void AllocDescriptorBlockForDraw()
    {
        if (_cbvBumpCursor + _cbvRangeSize > _cbvFrameStride
            || _srvBumpCursor + _srvRangeSize > _srvFrameStride
            || _uavBumpCursor + _uavRangeSize > _uavFrameStride
            || _samplerBumpCursor + _samplerRangeSize > _samplerFrameStride)
        {
            throw new InvalidOperationException(
                $"Exceeded the per-frame descriptor budget ({_drawsPerFrameCap} CBV/SRV/UAV draws, {_samplerDrawsPerFrameCap} sampler-using draws). " +
                "Increase the relevant capacity constant in D3DGraphicsDevice.");
        }

        uint frameCbvBase = _cbvRegionStart + _frameIndex * _cbvFrameStride;
        uint frameSrvBase = _srvRegionStart + _frameIndex * _srvFrameStride;
        uint frameUavBase = _uavRegionStart + _frameIndex * _uavFrameStride;
        uint frameSamplerBase = _frameIndex * _samplerFrameStride;

        _currentCbvBase = frameCbvBase + _cbvBumpCursor;
        _currentSrvBase = frameSrvBase + _srvBumpCursor;
        _currentUavBase = frameUavBase + _uavBumpCursor;
        _currentSamplerBase = frameSamplerBase + _samplerBumpCursor;

        _cbvBumpCursor += _cbvRangeSize;
        _srvBumpCursor += _srvRangeSize;
        _uavBumpCursor += _uavRangeSize;
        _samplerBumpCursor += _samplerRangeSize;

        _hasDescriptorBlock = true;
    }

    // Plain 2x2 box filter, one byte per channel. Clamps at the edge for odd dimensions (last
    // row/column gets sampled twice rather than reading out of bounds) - a correct, unsurprising
    // downsample, nothing fancier.
    private static byte[] DownsampleBox2x2(byte[] src, uint width, uint height, uint bytesPerPixel, out uint dstWidth, out uint dstHeight)
    {
        dstWidth = Math.Max(1u, width / 2);
        dstHeight = Math.Max(1u, height / 2);
        byte[] dst = new byte[dstWidth * dstHeight * bytesPerPixel];

        for (uint y = 0; y < dstHeight; y++)
        {
            uint srcY0 = Math.Min(y * 2, height - 1);
            uint srcY1 = Math.Min(y * 2 + 1, height - 1);

            for (uint x = 0; x < dstWidth; x++)
            {
                uint srcX0 = Math.Min(x * 2, width - 1);
                uint srcX1 = Math.Min(x * 2 + 1, width - 1);

                uint dstBase = (y * dstWidth + x) * bytesPerPixel;
                uint s00 = (srcY0 * width + srcX0) * bytesPerPixel;
                uint s10 = (srcY0 * width + srcX1) * bytesPerPixel;
                uint s01 = (srcY1 * width + srcX0) * bytesPerPixel;
                uint s11 = (srcY1 * width + srcX1) * bytesPerPixel;

                for (uint c = 0; c < bytesPerPixel; c++)
                {
                    int sum = src[s00 + c] + src[s10 + c] + src[s01 + c] + src[s11 + c];
                    dst[dstBase + c] = (byte)(sum / 4);
                }
            }
        }

        return dst;
    }

    private unsafe void UploadMipLevel(D3DImage dst, uint mip, byte[] tightlyPackedPixels, uint width, uint height)
    {
        ResourceDescription desc = dst.Resource->GetDesc();

        PlacedSubresourceFootprint footprint = default;
        ulong totalBytes;
        _device.Get()->GetCopyableFootprints(&desc, mip, 1, 0, &footprint, null, null, &totalBytes);

        uint tightRowPitch = width * D3DUtilities.GetBytesPerPixel(dst.Format);
        ulong paddedSize = footprint.Footprint.RowPitch * (ulong)footprint.Footprint.Height;

        D3DBuffer padded = new D3DBuffer(_allocator, paddedSize, BufferType.Upload, BufferUsage.CopySource);

        void* dstMapped;
        padded.Resource->Map(0, null, &dstMapped);

        fixed (byte* srcPtr = tightlyPackedPixels)
        {
            for (uint row = 0; row < height; row++)
            {
                Buffer.MemoryCopy(
                    srcPtr + row * tightRowPitch,
                    (byte*)dstMapped + row * footprint.Footprint.RowPitch,
                    footprint.Footprint.RowPitch,
                    tightRowPitch);
            }
        }

        padded.Resource->Unmap(0, null);

        ID3D12GraphicsCommandList* cmdList = BeginUpload();

        BarrierTransition(cmdList, dst.Resource, ref dst.State, ResourceStates.CopyDest);

        TextureCopyLocation dstLoc = new TextureCopyLocation
        {
            pResource = dst.Resource,
            Type = TextureCopyType.SubresourceIndex,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { SubresourceIndex = mip }
        };

        TextureCopyLocation srcLoc = new TextureCopyLocation
        {
            pResource = padded.Resource,
            Type = TextureCopyType.PlacedFootprint,
            Anonymous = new TextureCopyLocation._Anonymous_e__Union { PlacedFootprint = footprint }
        };

        cmdList->CopyTextureRegion(&dstLoc, 0, 0, 0, &srcLoc, null);

        ResourceStates finalState = (dst.Usage & ImageUsage.Sampled) != 0 ? ResourceStates.PixelShaderResource : ResourceStates.Common;
        BarrierTransition(cmdList, dst.Resource, ref dst.State, finalState);

        EndUploadAndWait();
        padded.Dispose();
    }

    private static bool IsByteUNormFormat(ImageFormat format)
    {
        return format is ImageFormat.R8UNorm or ImageFormat.R8G8UNorm or ImageFormat.R8G8B8A8UNorm or ImageFormat.R8G8B8A8UNormSrgb;
    }

    private static unsafe void BarrierTransition(ID3D12GraphicsCommandList* cmdList, ID3D12Resource* resource, ref ResourceStates current, ResourceStates target)
    {
        if (current == target)
        {
            return;
        }

        cmdList->ResourceBarrierTransition(resource, current, target);
        current = target;
    }

    private static (Filter filter, float minLod, float maxLod) GetFilterMode(SamplerFilterMode mode)
    {
        bool anisotropic = (mode & (SamplerFilterMode.Anisotropic4x | SamplerFilterMode.Anisotropic8x | SamplerFilterMode.Anisotropic16x)) != 0;
        bool bilinear = anisotropic || (mode & SamplerFilterMode.Bilinear) != 0;
        bool mipLinear = anisotropic || (mode & SamplerFilterMode.MipmapBilinear) != 0;
        bool mipNearest = !anisotropic && !mipLinear && (mode & SamplerFilterMode.MipmapNearest) != 0;
        bool mipEnabled = mipLinear || mipNearest;

        Filter filter;

        if (!bilinear && !mipLinear)
        {
            filter = Filter.MinMagMipPoint;
        }
        else if (!bilinear && mipLinear)
        {
            filter = Filter.MinMagPointMipLinear;
        }
        else if (bilinear && !mipLinear)
        {
            filter = Filter.MinMagLinearMipPoint;
        }
        else
        {
            filter = Filter.MinMagMipLinear;
        }

        float minLod = 0f;
        float maxLod = mipEnabled ? float.MaxValue : 0f;

        return (filter, minLod, maxLod);
    }

    private static TextureAddressMode GetWrapMode(SamplerWrapMode mode)
    {
        return mode switch
        {
            SamplerWrapMode.Repeat => TextureAddressMode.Wrap,
            SamplerWrapMode.Clamp => TextureAddressMode.Clamp,
            SamplerWrapMode.Mirror => TextureAddressMode.Mirror,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    #endregion

    // HAULT!! IF YOU VALUE YOUR SANITY, TURN BACK NOW! 
    // BELOW THIS LINE IS A MESS OF D3D GARBLE THAT ONLY MAKES SENSE TO COMPUTERS, AND THOSE WITH THE BRAIN OF A COMPUTER.
    // AT RISK OF BECOMING MACHINE, TURN BACK NOW!

    #region D3D Init
    private unsafe void InitDebug()
    {
        if (_isDebug)
        {
            ID3D12Debug3* debug;

            fixed (Guid* gptr = &ID3D12Debug3.IID_ID3D12Debug3)
            {
                if (D3D12GetDebugInterface(gptr, (void**)&debug) != HResult.Ok)
                {
                    throw new InvalidOperationException("Failed to initialize D3D debug layer!");
                }
            }

            debug->EnableDebugLayer();
            debug->SetEnableGPUBasedValidation(true);
            debug->SetEnableSynchronizedCommandQueueValidation(true);
            debug->Release();
        }
    }

    private unsafe void InitFactory()
    {
        IDXGIFactory* tempFact;

        fixed (Guid* gptr = &IDXGIFactory.IID_IDXGIFactory)
        {
            if (CreateDXGIFactory2(_isDebug, gptr, (void**)&tempFact) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize DXGI factory!");
            }
        }

        IDXGIFactory7* factory;

        fixed (Guid* gptr = &IDXGIFactory7.IID_IDXGIFactory7)
        {
            if (tempFact->QueryInterface(gptr, (void**)&factory) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest DXGI adapter interface!");
            }
        }

        tempFact->Release();
        _factory.Attach(factory);
    }

    private unsafe void InitAdapter()
    {
        IDXGIAdapter1* bestAdp = null;
        ulong bestVram = 0;

        for (uint i = 0; true; i++)
        {
            IDXGIAdapter1* tempAdp = null;

            if (_factory.Get()->EnumAdapters1(i, &tempAdp) == (HResult)0x887A0002)
            {
                break;
            }

            AdapterDescription1 desc;
            tempAdp->GetDesc1(&desc);

            if ((desc.Flags & AdapterFlags.Software) != 0)
            {
                continue;
            }

            ID3D12Device8* tempDev;

            fixed (Guid* gptr = &ID3D12Device8.IID_ID3D12Device8)
            {
                if (D3D12CreateDevice((IUnknown*)tempAdp, _featLevel, gptr, (void**)&tempDev) != HResult.Ok)
                {
                    tempAdp->Release();
                    continue;
                }
            }

            tempDev->Release();

            if (bestAdp == null || bestVram < desc.DedicatedVideoMemory)
            {
                if (bestAdp != null)
                {
                    bestAdp->Release();
                }

                bestAdp = tempAdp;
                bestVram = desc.DedicatedVideoMemory;
            }
            else
            {
                tempAdp->Release();
            }
        }

        if (bestAdp == null)
        {
            throw new InvalidOperationException("Failed to find a suitable DXGI adapter!");
        }

        IDXGIAdapter4* adapter;

        fixed (Guid* gptr = &IDXGIAdapter4.IID_IDXGIAdapter4)
        {
            if (bestAdp->QueryInterface(gptr, (void**)&adapter) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest DXGI adapter interface!");
            }
        }

        bestAdp->Release();
        _adapter.Attach(adapter);
    }

    private unsafe void InitDevice()
    {
        ID3D12Device* tempDev;

        fixed (Guid* gptr = &ID3D12Device.IID_ID3D12Device)
        {
            if (D3D12CreateDevice((IUnknown*)_adapter.Get(), _featLevel, gptr, (void**)&tempDev) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D device!");
            }
        }

        ID3D12Device8* device;

        fixed (Guid* gptr = &ID3D12Device8.IID_ID3D12Device8)
        {
            if (tempDev->QueryInterface(gptr, (void**)&device) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest D3D device interface!");
            }
        }

        tempDev->Release();
        _device.Attach(device);
    }

    private unsafe void InitInfoQueue()
    {
        if (!_isDebug)
        {
            return;
        }

        ID3D12InfoQueue* infoQueue;
        IDXGIInfoQueue* dxgiInfoQueue;

        fixed (Guid* gptr = &ID3D12InfoQueue.IID_ID3D12InfoQueue)
        {
            if (_device.Get()->QueryInterface(gptr, (void**)&infoQueue) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D info queue!");
            }
        }

        fixed (Guid* gptr = &IDXGIInfoQueue.IID_IDXGIInfoQueue)
        {
            if (DXGIGetDebugInterface1(0, gptr, (void**)&dxgiInfoQueue) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize DXGI info queue!");
            }
        }

        MessageSeverity* allowSev = stackalloc MessageSeverity[3]
        {
            MessageSeverity.Corruption,
            MessageSeverity.Error,
            MessageSeverity.Warning,
        };


        InfoQueueMessageSeverity* dxgiAllowSev = stackalloc InfoQueueMessageSeverity[3]
        {
            InfoQueueMessageSeverity.Corruption,
            InfoQueueMessageSeverity.Error,
            InfoQueueMessageSeverity.Warning
        };

        Vortice.Win32.Graphics.Direct3D12.MessageId* denyIds = stackalloc Vortice.Win32.Graphics.Direct3D12.MessageId[1]
        {
            Vortice.Win32.Graphics.Direct3D12.MessageId.ClearRenderTargetViewMismatchingClearValue,
        };

        Vortice.Win32.Graphics.Direct3D12.InfoQueueFilter filter = new()
        {
            AllowList = new()
            {
                NumSeverities = 3,
                pSeverityList = allowSev,
            },
            DenyList = new()
            {
                NumIDs = 1,
                pIDList = denyIds,
            }
        };

        Vortice.Win32.Graphics.Dxgi.InfoQueueFilter dxgiFilter = new()
        {
            AllowList = new()
            {
                NumSeverities = 3,
                pSeverityList = dxgiAllowSev,
            },
        };

        infoQueue->PushStorageFilter(&filter);
        dxgiInfoQueue->PushStorageFilter(DXGI_DEBUG_DXGI, &dxgiFilter);
        infoQueue->SetMuteDebugOutput(false);
        dxgiInfoQueue->SetMuteDebugOutput(DXGI_DEBUG_DXGI, false);
        infoQueue->SetMessageCountLimit(ulong.MaxValue);
        dxgiInfoQueue->SetMessageCountLimit(DXGI_DEBUG_DXGI, ulong.MaxValue);
        _mainInfoQueue.Attach(infoQueue);
        _dxgiInfoQueue.Attach(dxgiInfoQueue);
    }

    private unsafe void InitCommandQueue()
    {
        ID3D12CommandQueue* cmdQueue;
        CommandQueueDescription desc = new CommandQueueDescription()
        {
            Type = CommandListType.Direct,
            Flags = CommandQueueFlags.None
        };

        fixed (Guid* gptr = &ID3D12CommandQueue.IID_ID3D12CommandQueue)
        {
            if (_device.Get()->CreateCommandQueue(&desc, gptr, (void**)&cmdQueue) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D command queue!");
            }
        }

        _cmdQueue.Attach(cmdQueue);
    }

    private unsafe void InitSwapChain()
    {
        IDXGISwapChain1* tempChain;
        SwapChainDescription1 desc = new SwapChainDescription1()
        {
            Width = (uint)Game.Instance!.Window.Resolution.W,
            Height = (uint)Game.Instance!.Window.Resolution.H,
            Format = Format.R8G8B8A8Unorm,
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = _frameCount,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDescription()
            {
                Count = 1,
                Quality = 0
            },
            Flags = SwapChainFlags.AllowTearing,
        };

        uint props = SDL.GetWindowProperties(Game.Instance.Window.Handle);
        nint hwnd = (nint)SDL.GetPointerProperty(props, SDL.SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);

        fixed (Guid* gptr = &IDXGISwapChain1.IID_IDXGISwapChain1)
        {
            if (_factory.Get()->CreateSwapChainForHwnd((IUnknown*)_cmdQueue.Get(), hwnd, &desc, null, null, &tempChain) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize DXGI swapchain!");
            }
        }

        IDXGISwapChain4* swapChain;

        fixed (Guid* gptr = &IDXGISwapChain4.IID_IDXGISwapChain4)
        {
            if (tempChain->QueryInterface(gptr, (void**)&swapChain) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest DXGI swapchain interface!");
            }
        }

        tempChain->Release();
        _swapChain.Attach(swapChain);
    }

    private unsafe void InitBackBuffer()
    {
        DescriptorHeapDescription heapDesc = new DescriptorHeapDescription()
        {
            NumDescriptors = _frameCount,
            Type = DescriptorHeapType.Rtv,
            Flags = DescriptorHeapFlags.None,
        };

        RenderTargetViewDescription targDesc = new RenderTargetViewDescription()
        {
            Format = Format.R8G8B8A8Unorm,
            ViewDimension = RtvDimension.Texture2D
        };

        ID3D12DescriptorHeap* renderHeap;

        fixed (Guid* gptr = &ID3D12DescriptorHeap.IID_ID3D12DescriptorHeap)
        {
            if (_device.Get()->CreateDescriptorHeap(&heapDesc, gptr, (void**)&renderHeap) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to create D3D render heap!");
            }
        }

        _renderHeap.Attach(renderHeap);
        _renderHeapSize = _device.Get()->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);

        ID3D12Resource** backBuffers = stackalloc ID3D12Resource*[(int)_frameCount];

        for (uint i = 0; i < _frameCount; i++)
        {
            fixed (Guid* gptr = &ID3D12Resource.IID_ID3D12Resource)
            {
                if (_swapChain.Get()->GetBuffer(i, gptr, (void**)&backBuffers[i]) != HResult.Ok)
                {
                    throw new InvalidOperationException("Failed to create D3D back buffers!");
                }
            }

            fixed (char* name = $"BackBuffer{i}")
            {
                backBuffers[i]->SetName((char*)name);
            }
        }

        CpuDescriptorHandle start = _renderHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        uint size = _device.Get()->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);

        for (uint j = 0; j < _frameCount; j++)
        {
            CpuDescriptorHandle handle = start;
            handle.ptr += (nuint)(j * size);
            _device.Get()->CreateRenderTargetView(backBuffers[j], &targDesc, handle);
        }

        ComPtr<ID3D12Resource>[] bbPtrs = new ComPtr<ID3D12Resource>[_frameCount];

        for (int k = 0; k < _frameCount; k++)
        {
            bbPtrs[k].Attach(backBuffers[k]);
        }

        _backBuffers = bbPtrs;
    }

    private unsafe void InitCommandAllocator()
    {
        _mainCmdAllocs = new ComPtr<ID3D12CommandAllocator>[_frameCount];

        for (uint i = 0; i < _frameCount; i++)
        {
            ID3D12CommandAllocator* cmdAlloc;

            fixed (Guid* gptr = &ID3D12CommandAllocator.IID_ID3D12CommandAllocator)
            {
                if (_device.Get()->CreateCommandAllocator(CommandListType.Direct, gptr, (void**)&cmdAlloc) != HResult.Ok)
                {
                    throw new InvalidOperationException("Failed to initialize D3D command allocator!");
                }
            }

            cmdAlloc->Reset();
            _mainCmdAllocs[i].Attach(cmdAlloc);
        }

        ID3D12CommandAllocator* uploadAlloc;

        fixed (Guid* gptr = &ID3D12CommandAllocator.IID_ID3D12CommandAllocator)
        {
            if (_device.Get()->CreateCommandAllocator(CommandListType.Direct, gptr, (void**)&uploadAlloc) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D upload command allocator!");
            }
        }

        uploadAlloc->Reset();
        _uploadCmdAlloc.Attach(uploadAlloc);
    }

    private unsafe void InitCommandList()
    {
        ID3D12GraphicsCommandList* tempList;
        ID3D12GraphicsCommandList* uploadTempList;

        fixed (Guid* gptr = &ID3D12GraphicsCommandList.IID_ID3D12GraphicsCommandList)
        {
            if (_device.Get()->CreateCommandList(0, CommandListType.Direct, _mainCmdAllocs[0].Get(), null, gptr, (void**)&tempList) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D command list!");
            }

            if (_device.Get()->CreateCommandList(0, CommandListType.Direct, _uploadCmdAlloc.Get(), null, gptr, (void**)&uploadTempList) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D upload command list!");
            }
        }

        ID3D12GraphicsCommandList6* cmdList;
        ID3D12GraphicsCommandList6* uploadList;

        fixed (Guid* gptr = &ID3D12GraphicsCommandList6.IID_ID3D12GraphicsCommandList6)
        {
            if (tempList->QueryInterface(gptr, (void**)&cmdList) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest D3D command list interface!");
            }

            if (uploadTempList->QueryInterface(gptr, (void**)&uploadList) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest D3D upload command list interface!");
            }
        }

        tempList->Release();
        uploadTempList->Release();
        cmdList->Close();
        uploadList->Close();
        _mainCmdList.Attach(cmdList);
        _uploadCmdList.Attach(uploadList);
    }

    private unsafe void InitFence()
    {
        ID3D12Fence* tempFen;
        ID3D12Fence* uploadTempFen;

        fixed (Guid* gptr = &ID3D12Fence.IID_ID3D12Fence)
        {
            if (_device.Get()->CreateFence(0, FenceFlags.None, gptr, (void**)&tempFen) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D fence!");
            }

            if (_device.Get()->CreateFence(0, FenceFlags.None, gptr, (void**)&uploadTempFen) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to initialize D3D upload fence!");
            }
        }

        ID3D12Fence1* fence;
        ID3D12Fence1* uploadFence;

        fixed (Guid* gptr = &ID3D12Fence1.IID_ID3D12Fence1)
        {
            if (tempFen->QueryInterface(gptr, (void**)&fence) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest D3D fence interface!");
            }

            if (uploadTempFen->QueryInterface(gptr, (void**)&uploadFence) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed query for latest D3D upload fence interface!");
            }
        }

        tempFen->Release();
        uploadTempFen->Release();
        _mainFence.Attach(fence);
        _uploadFence.Attach(uploadFence);
    }

    private unsafe void InitMemoryAllocator()
    {
        AllocatorDesc desc = new AllocatorDesc()
        {
            pDevice = (ID3D12Device*)_device.Get(),
            pAdapter = (IDXGIAdapter*)_adapter.Get(),
            PreferredBlockSize = 65536,
            Flags = AllocatorFlags.None,
        };

        Allocator allocator;

        if (CreateAllocator(desc, out allocator) != HResult.Ok)
        {
            throw new InvalidOperationException("Failed to initialize D3D memory allocator!");
        }

        _allocator = allocator;
    }

    private unsafe void QueryGpuInfo()
    {
        FeatureLevel* possibleLvls = stackalloc FeatureLevel[5]
        {
            FeatureLevel.Level_12_2,
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0
        };

        FeatureDataFeatureLevels featureLvls = new FeatureDataFeatureLevels()
        {
            NumFeatureLevels = 5,
            pFeatureLevelsRequested = possibleLvls
        };

        AdapterDescription3 adpDesc;
        QueryVideoMemoryInfo vramInfo;
        _adapter.Get()->GetDesc3(&adpDesc);
        _adapter.Get()->QueryVideoMemoryInfo(0, MemorySegmentGroup.Local, &vramInfo);
        _device.Get()->CheckFeatureSupport(Vortice.Win32.Graphics.Direct3D12.Feature.FeatureLevels, &featureLvls, sizeof(FeatureDataFeatureLevels));

        long umdVer;
        (ushort A, ushort B, ushort C, ushort D) unpacked;

        fixed (Guid* gptr = &IDXGIDevice.IID_IDXGIDevice)
        {
            _adapter.Get()->CheckInterfaceSupport(gptr, &umdVer);
        }

        unpacked.A = (ushort)((umdVer >> 48) & 0xFFFF);
        unpacked.B = (ushort)((umdVer >> 32) & 0xFFFF);
        unpacked.C = (ushort)((umdVer >> 16) & 0xFFFF);
        unpacked.D = (ushort)(umdVer & 0xFFFF);

        Logger.AppendLog("D3D",
            "Successfully initalized Direct3D!", ConsoleColor.DarkCyan, 1);
        Logger.AppendLog("D3D",
            $"   > GPU: {Marshal.PtrToStringUni((nint)adpDesc.Description)} (driver {unpacked.A}.{unpacked.B}.{unpacked.C}.{unpacked.D}) ", ConsoleColor.DarkCyan, 1);
        Logger.AppendLog("D3D",
            $"   > VRAM: {Math.Round(adpDesc.DedicatedVideoMemory * 1e-6)} MB ({Math.Round(vramInfo.Budget * 1e-6)} MB available)", ConsoleColor.DarkCyan, 1);
        Logger.AppendLog("D3D",
            $"   > FL: {_featLevel} ({featureLvls.MaxSupportedFeatureLevel} available)", ConsoleColor.DarkCyan, 1);
    }
    #endregion
}