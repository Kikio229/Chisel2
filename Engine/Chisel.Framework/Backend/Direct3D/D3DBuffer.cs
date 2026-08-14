using System;
using Vortice.Win32;
using Vortice.Win32.Graphics.D3D12MemoryAllocator;
using Vortice.Win32.Graphics.Direct3D12;

namespace Chisel.Framework;

internal class D3DBuffer : Disposable, IBuffer
{
    public ulong Size { get; }
    public BufferType Type { get; }
    public BufferUsage Usage { get; }

    internal Allocation Allocation { get; }
    internal unsafe ID3D12Resource* Resource { get; }
    internal ResourceStates State;

    public unsafe D3DBuffer(Allocator allocator, ulong size, BufferType type, BufferUsage usage)
    {
        Size = size;
        Type = type;
        Usage = usage;

        ResourceDescription resDesc = ResourceDescription.Buffer(new ResourceAllocationInfo()
        {
            Alignment = 65536,
            SizeInBytes = Size,
        });

        AllocationDesc allocDesc = new AllocationDesc()
        {
            HeapType = D3DUtilities.GetHeapTypeFromBuffer(Type)
        };

        ResourceStates state = D3DUtilities.GetResourceStateFromBuffer(Type);

        Allocation allocation;
        ID3D12Resource* resource;

        fixed (Guid* gptr = &ID3D12Resource.IID_ID3D12Resource)
        {
            if (allocator.CreateResource(&allocDesc, resDesc, state, null, &allocation, gptr, (void**)&resource) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to allocate buffer memory!");
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
