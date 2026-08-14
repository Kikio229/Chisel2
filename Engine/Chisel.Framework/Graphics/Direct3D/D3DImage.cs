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
    internal Allocation Allocation { get; }
    internal unsafe ID3D12Resource* Resource { get; }
    internal uint SampleCount { get; }
    internal ResourceStates State;

    public unsafe D3DImage(Allocator allocator, uint width, uint height, uint mips, ImageFormat format, ImageUsage usage, uint sampleCount = 1)
    {
        Width = width;
        Height = height;
        MipLevels = mips;
        Format = format;
        Usage = usage;
        SampleCount = sampleCount;

        Format dfmt = GetDxgiFormatFromImage(Format);

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
            Resource->Release();
        }
    }

    internal static Format GetDxgiFormatFromImage(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Unknown => Vortice.Win32.Graphics.Dxgi.Common.Format.Unknown,
            ImageFormat.R8UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R8Unorm,
            ImageFormat.R8G8UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R8G8Unorm,
            ImageFormat.R8G8B8A8UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R8G8B8A8Unorm,
            ImageFormat.R8G8B8A8UNormSrgb => Vortice.Win32.Graphics.Dxgi.Common.Format.R8G8B8A8UnormSrgb,
            ImageFormat.R16UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R16Unorm,
            ImageFormat.R16G16UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R16G16Unorm,
            ImageFormat.R16G16B16A16UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.R16G16B16A16Unorm,
            ImageFormat.R16Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R16Float,
            ImageFormat.R16G16Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R16G16Float,
            ImageFormat.R16G16B16A16Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R16G16B16A16Float,
            ImageFormat.R32Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R32Float,
            ImageFormat.R32G32Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32Float,
            ImageFormat.R32G32B32Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32B32Float,
            ImageFormat.R32G32B32A32Float => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32B32A32Float,
            ImageFormat.R32UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.R32Uint,
            ImageFormat.R32G32UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32Uint,
            ImageFormat.R32G32B32UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32B32Uint,
            ImageFormat.R32G32B32A32UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.R32G32B32A32Uint,
            ImageFormat.D16UNorm => Vortice.Win32.Graphics.Dxgi.Common.Format.D16Unorm,
            ImageFormat.D24UNormS8UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.D24UnormS8Uint,
            ImageFormat.D32Float => Vortice.Win32.Graphics.Dxgi.Common.Format.D32Float,
            ImageFormat.D32FloatS8UInt => Vortice.Win32.Graphics.Dxgi.Common.Format.D32FloatS8X24Uint,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Image format is unknown or invalid!")
        };
    }
    internal static uint GetBytesPerPixel(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8UNorm => 1,
            ImageFormat.R8G8UNorm => 2,
            ImageFormat.R8G8B8A8UNorm or ImageFormat.R8G8B8A8UNormSrgb => 4,
            ImageFormat.R16UNorm or ImageFormat.R16Float => 2,
            ImageFormat.R16G16UNorm or ImageFormat.R16G16Float => 4,
            ImageFormat.R16G16B16A16UNorm or ImageFormat.R16G16B16A16Float => 8,
            ImageFormat.R32Float or ImageFormat.R32UInt => 4,
            ImageFormat.R32G32Float or ImageFormat.R32G32UInt => 8,
            ImageFormat.R32G32B32Float or ImageFormat.R32G32B32UInt => 12,
            ImageFormat.R32G32B32A32Float or ImageFormat.R32G32B32A32UInt => 16,
            ImageFormat.D16UNorm => 2,
            ImageFormat.D24UNormS8UInt or ImageFormat.D32Float => 4,
            ImageFormat.D32FloatS8UInt => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Image format is unknown or invalid!")
        };
    }
}
