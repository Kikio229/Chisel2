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

    readonly bool useRing;
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

        useRing = device.Backend == GraphicsBackend.Direct3D12;

        if (useRing)
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
        IBuffer target;

        if (useRing)
        {
            uint frameIndex = GraphicsDevice.CurrentFrameIndex;

            if (frameIndex != lastSeenFrameIndex)
            {
                lastSeenFrameIndex = frameIndex;
                lanePositions[frameIndex] = 0;
            }

            IBuffer[] lane = laneRings[frameIndex];
            int slotIndex = lanePositions[frameIndex]++;

            if (slotIndex >= lane.Length)
            {
                // Only grows the first time this lane needs this many binds in a single frame -
                // once steady state is reached (same call count every frame), this stops firing
                // entirely and every later frame just reuses the same physical buffers.
                Array.Resize(ref lane, slotIndex + 1);
                lane[slotIndex] = AllocateSlot();
                laneRings[frameIndex] = lane;
            }
            else
            {
                GraphicsDevice.UpdateBuffer(lane[slotIndex], data, 0);
            }

            target = lane[slotIndex];
        }
        else
        {
            target = nonRingBuffer;

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

        if (useRing)
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