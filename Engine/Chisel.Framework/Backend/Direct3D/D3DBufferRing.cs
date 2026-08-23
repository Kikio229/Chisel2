using System;
using System.Collections.Generic;
using Vortice.Win32.Graphics.D3D12MemoryAllocator;

namespace Chisel.Framework;

internal class D3DBufferRing : Disposable
{
    public ulong Cursor { get; private set; }
    public ulong Capacity { get; private set; }
    public ulong Overflow { get; private set; }
    public int OverflowCount => _overflow.Count;

    private ulong _pendingCap;
    private unsafe void* _mapped;
    private D3DBuffer? _arena;
    private List<D3DBuffer> _overflow;

    private const ulong Alignment = 256; // D3D12 CBV alignment requirement
    private readonly Allocator _allocator;

    public unsafe D3DBufferRing(Allocator allocator, ulong capacity)
    {
        Cursor = 0;
        Capacity = 0;
        Overflow = 0;

        _pendingCap = 0;
        _mapped = null;
        _arena = null;
        _overflow = new List<D3DBuffer>();

        _allocator = allocator;
        CreateArena(capacity);
    }

    public unsafe void Begin()
    {
        foreach (D3DBuffer b in _overflow)
        {
            b.Dispose();
        }

        _overflow.Clear();
        Overflow = 0;

        if (_pendingCap > Capacity)
        {
            _arena?.Resource->Unmap(0, null);
            _arena?.Dispose();
            CreateArena(_pendingCap);
            _pendingCap = 0;
        }

        Cursor = 0;
    }

    public unsafe (IBuffer arena, ulong offset) AllocBuffer(ReadOnlySpan<byte> data)
    {
        ulong aligned = (Cursor + (Alignment - 1)) & ~(Alignment - 1);

        if (aligned + (ulong)data.Length > Capacity)
        {
            ulong paddedSize = ((ulong)data.Length + (Alignment - 1)) & ~(Alignment - 1);
            D3DBuffer fallback = new D3DBuffer(_allocator, paddedSize, BufferType.Upload, BufferUsage.Constant);

            void* fallbackMapped;
            fallback.Resource->Map(0, null, &fallbackMapped);
            data.CopyTo(new Span<byte>(fallbackMapped, data.Length));
            fallback.Resource->Unmap(0, null);

            _overflow.Add(fallback);
            Overflow += paddedSize;

            return (fallback, 0);
        }

        data.CopyTo(new Span<byte>((byte*)_mapped + aligned, data.Length));
        Cursor = aligned + (ulong)data.Length;

        return (_arena!, aligned);
    }

    public void RequestGrow(ulong capacity)
    {
        if (capacity > _pendingCap)
        {
            _pendingCap = capacity;
        }
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (disposing)
        {
            _arena?.Resource->Unmap(0, null);
            _arena?.Dispose();

            foreach (D3DBuffer b in _overflow)
            {
                b.Dispose();
            }

            _overflow.Clear();
        }
    }

    private unsafe void CreateArena(ulong capacity)
    {
        _arena = new D3DBuffer(_allocator, capacity, BufferType.Upload, BufferUsage.Constant);

        void* mapped;
        _arena.Resource->Map(0, null, &mapped);

        _mapped = mapped;
        Capacity = capacity;
    }
}