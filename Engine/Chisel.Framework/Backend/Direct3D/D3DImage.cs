using System;
using Vortice.Win32;
using Vortice.Win32.Graphics.D3D12MemoryAllocator;
using Vortice.Win32.Graphics.Direct3D12;
using Vortice.Win32.Graphics.Dxgi;
using Vortice.Win32.Graphics.Dxgi.Common;

namespace Chisel.Framework;

internal class D3DImage : Disposable, IImage
{
    public uint Width { get; }
    public uint Height { get; }
    public uint MipLevels { get; }
    public ImageFormat Format { get; }
    public ImageUsage Usage { get; }

    internal uint SampleCount { get; }
    internal Allocation Allocation { get; }
    internal unsafe ID3D12Resource* Resource { get; }
    internal ResourceStates State;

    public unsafe D3DImage(Allocator allocator, uint width, uint height, uint mips, ImageFormat format, ImageUsage usage, uint sampleCount = 1)
    {
        Width = width;
        Height = height;
        MipLevels = mips;
        Format = format;
        Usage = usage;
        SampleCount = sampleCount;

        Format dfmt = D3DUtilities.GetDxgiFormatFromImage(Format);

        ResourceFlags flags = ResourceFlags.None;
        ResourceStates state = ResourceStates.Common;
        ClearValue? clear = null;

        if ((Usage & ImageUsage.Sampled) != 0)
        {
            // I don't think i need i need to do anything special
            // for sampled images?
        }

        if ((Usage & ImageUsage.RenderTarget) != 0)
        {
            flags |= ResourceFlags.AllowRenderTarget;
            state = ResourceStates.RenderTarget;
        }

        if ((Usage & ImageUsage.DepthStencil) != 0)
        {
            flags |= ResourceFlags.AllowDepthStencil;
            state = ResourceStates.DepthWrite;
            float* color = stackalloc float[4] { 0, 0, 0, 1.0f };
            ClearValue depthClear = new ClearValue(dfmt, color);
            depthClear.DepthStencil = new DepthStencilValue(1.0f, 0);
            clear = depthClear;
        }

        if ((Usage & ImageUsage.Storage) != 0)
        {
            flags |= ResourceFlags.AllowUnorderedAccess;
            state = ResourceStates.UnorderedAccess;
        }

        ResourceDescription resDesc = ResourceDescription.Tex2D(dfmt, Width, Height, 1, (ushort)MipLevels, sampleCount, 0, flags);

        AllocationDesc allocDesc = new AllocationDesc()
        {
            HeapType = HeapType.Default
        };

        Allocation allocation;
        ID3D12Resource* resource;

        fixed (Guid* gptr = &ID3D12Resource.IID_ID3D12Resource)
        {
            ClearValue c = clear.GetValueOrDefault();
            ClearValue* cptr = (clear.HasValue) ? &c : null;

            if (allocator.CreateResource(&allocDesc, resDesc, state, cptr, &allocation, gptr, (void**)&resource) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to allocate image memory!");
            }
        }

        Allocation = allocation;
        Resource = resource;
        State = state;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (disposing)
        {
            Allocation.Release();
            Resource->Release();
        }
    }
}
