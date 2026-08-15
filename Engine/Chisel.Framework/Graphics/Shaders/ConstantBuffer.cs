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

    readonly bool useArena;
    IBuffer nonRingBuffer; // GL path only: single reused buffer, driver-orphaning handles the rest

    // D3D path only: one independent slot list + write-cursor per frame-in-flight lane. A lane
    // maps 1:1 to D3DGraphicsDevice's back-buffer index. Reusing lane N's slots is only safe once
    // BeginFrame has already fence-waited for lane N's prior GPU work to finish - which the device
    // already guarantees has happened by the time FlushAndBind can be called again for that lane.
    const int MaxLanes = 2; // matches D3DGraphicsDevice's _maxFramesInFlight
    IBuffer[][] laneRings;
    int[] lanePositions;
    uint lastSeenFrameIndex = uint.MaxValue;

    internal IGraphicsDevice GraphicsDevice;

    public ConstantBuffer(string name, uint slot, int sizeInBytes, IGraphicsDevice device)
    {
        Name = name;
        Slot = slot;
        GraphicsDevice = device;
        data = new byte[sizeInBytes];

        useArena = device.Backend == GraphicsBackend.Direct3D12;

        if (useArena)
        {
            laneRings = new IBuffer[MaxLanes][];
            lanePositions = new int[MaxLanes];

            for (int lane = 0; lane < MaxLanes; lane++)
            {
                laneRings[lane] = Array.Empty<IBuffer>();
            }
        }
        else
        {
            nonRingBuffer = AllocateSlot();
        }
    }

    IBuffer AllocateSlot()
    {
        IBuffer buffer = GraphicsDevice.CreateBuffer(new BufferDescription
        {
            Size = (ulong)data.Length,
            Type = BufferType.Upload,
            Usage = BufferUsage.Constant,
        });
        GraphicsDevice.UpdateBuffer(buffer, data, 0);
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
        if (useArena)
        {
            var (arena, offset) = GraphicsDevice.SuballocateBuffer(data);
            GraphicsDevice.BindConstantBuffer(arena, offset, (uint)data.Length, Slot);
        }
        else
        {
            if (dirty) GraphicsDevice.UpdateBuffer(nonRingBuffer, data, 0);
            GraphicsDevice.BindConstantBuffer(nonRingBuffer, Slot);
        }

        dirty = false;
    }

    public void Dispose()
    {
        if (disposedValue)
        {
            return;
        }

        if (useArena)
        {
            foreach (IBuffer[] lane in laneRings)
            {
                foreach (IBuffer buffer in lane)
                {
                    (buffer as IDisposable)?.Dispose();
                }
            }
        }
        else
        {
            (nonRingBuffer as IDisposable)?.Dispose();
        }

        disposedValue = true;
    }
}