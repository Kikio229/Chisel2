using Chisel.Resource;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

public class ConstantBuffer : IDisposable
{
    public string Name { get; }
    public uint Slot { get; }

    byte[] data;
    bool dirty;
    bool disposedValue;

    // On D3D12 a command list is recorded now but only executed later, at EndFrame.
    readonly bool useRing;
    IBuffer[] ring;
    int ringCount;   // how many slots are actually allocated
    int ringIndex = -1;

    internal IGraphicsDevice GraphicsDevice;

    public ConstantBuffer(string name, uint slot, int sizeInBytes, IGraphicsDevice device)
    {
        Name = name;
        Slot = slot;
        GraphicsDevice = device;
        data = new byte[sizeInBytes];

        // Only D3D actually needs the ring. GL's driver-level orphaning already gives correct
        // per-draw semantics with a single buffer
        useRing = device.Backend == GraphicsBackend.Direct3D12;

        ring = new IBuffer[useRing ? 4 : 1]; // small initial capacity, grows on demand
        AllocateRingSlot(0);
        ringCount = 1;
        ringIndex = 0;
    }

    IBuffer AllocateRingSlot(int index)
    {
        IBuffer buffer = GraphicsDevice.CreateBuffer(new BufferDescription
        {
            Size = (ulong)data.Length,
            Type = BufferType.Upload,
            Usage = BufferUsage.Constant,
        });
        GraphicsDevice.UpdateBuffer(buffer, data, 0);
        ring[index] = buffer;
        return buffer;
    }

    internal void Write<T>(int offset, in T value) where T : unmanaged
    {
        MemoryMarshal.Write(data.AsSpan(offset, Unsafe.SizeOf<T>()), ref Unsafe.AsRef(value));
        dirty = true;
    }

    internal void WriteArray<T>(int offset, ReadOnlySpan<T> values) where T : unmanaged
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(values);
        bytes.CopyTo(data.AsSpan(offset, bytes.Length));
        dirty = true;
    }

    internal void FlushAndBind()
    {
        IBuffer target;

        if (useRing)
        {
            // Every bind is a distinct logical use of this cbuffer's current bytes, so always advance
            // to a fresh physical slot
            ringIndex++;

            if (ringIndex >= ringCount)
            {
                if (ringIndex >= ring.Length)
                {
                    Array.Resize(ref ring, ring.Length * 2);
                }

                target = AllocateRingSlot(ringIndex);
                ringCount = ringIndex + 1;
            }
            else
            {
                target = ring[ringIndex];
                GraphicsDevice.UpdateBuffer(target, data, 0);
            }
        }
        else
        {
            target = ring[0];

            if (dirty)
            {
                GraphicsDevice.UpdateBuffer(target, data, 0);
            }
        }

        dirty = false;
        GraphicsDevice.BindConstantBuffer(target, Slot);
    }

    public void Dispose()
    {
        if (disposedValue)
        {
            return;
        }

        for (int i = 0; i < ringCount; i++)
        {
            if (ring[i] is IDisposable disposableBuffer)
            {
                disposableBuffer.Dispose();
            }
        }

        disposedValue = true;
    }
}