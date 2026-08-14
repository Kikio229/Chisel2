using System;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D12;

namespace Chisel.Framework;

internal class D3DDescriptorHeap : Disposable
{
    public uint Size { get; }
    public uint Capacity { get; }
    internal unsafe ID3D12DescriptorHeap* Heap { get; }

    public unsafe D3DDescriptorHeap(ID3D12Device* device, DescriptorHeapType type, uint capacity, bool shaderVisible = true)
    {
        Size = device->GetDescriptorHandleIncrementSize(type);
        Capacity = capacity;

        // RTV/DSV heaps must never be shader-visible. D3D12 rejects the flag on those two
        // heap types outright. Only CbvSrvUav and Sampler heaps can be bound directly by shaders.
        DescriptorHeapDescription desc = new DescriptorHeapDescription()
        {
            Type = type,
            NumDescriptors = capacity,
            Flags = shaderVisible ? DescriptorHeapFlags.ShaderVisible : DescriptorHeapFlags.None
        };

        ID3D12DescriptorHeap* heap;

        fixed (Guid* gptr = &ID3D12DescriptorHeap.IID_ID3D12DescriptorHeap)
        {
            if (device->CreateDescriptorHeap(&desc, gptr, (void**)&heap) != HResult.Ok)
            {
                D3DGraphicsDevice.DumpDebugMessages(device);
                throw new InvalidOperationException("Failed to create D3D descriptor heap!");
            }
        }

        Heap = heap;
    }

    public unsafe CpuDescriptorHandle GetCpuStart()
    {
        return Heap->GetCPUDescriptorHandleForHeapStart();   
    }

    public CpuDescriptorHandle GetCpuAt(uint index)
    {
        if (index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        CpuDescriptorHandle handle = GetCpuStart();
        handle.ptr += index * Size;
        return handle;
    }

    public unsafe GpuDescriptorHandle GetGpuStart()
    {
        return Heap->GetGPUDescriptorHandleForHeapStart();
    }

    public GpuDescriptorHandle GetGpuAt(uint index)
    {
        if (index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        GpuDescriptorHandle handle = GetGpuStart();
        handle.ptr += index * Size;
        return handle;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (disposing)
        {
            Heap->Release();
        }
    }
}
