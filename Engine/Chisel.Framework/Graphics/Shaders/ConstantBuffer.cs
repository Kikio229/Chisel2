using Chisel.Framework;
using Silk.NET.OpenGL;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

public class ConstantBuffer
{
    public string Name { get; }
    public uint Slot { get; }
    byte[] data;
    bool dirty;

    internal IBuffer BackingBuffer;
    internal IGraphicsDevice GraphicsDevice;

    public ConstantBuffer(string name, uint slot, int sizeInBytes, IGraphicsDevice device)
    {
        Name = name;
        Slot = slot;
        GraphicsDevice = device;
        data = new byte[sizeInBytes];
        BackingBuffer = device.CreateBuffer(new BufferDescription
        {
            Size = (ulong)sizeInBytes,
            Type = BufferType.Upload,
            Usage = BufferUsage.Constant,
        });
        GraphicsDevice.UpdateBuffer(BackingBuffer, data, 0);
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
        if (dirty)
        {
            GraphicsDevice.UpdateBuffer(BackingBuffer, data, 0);
            dirty = false;
        }
        GraphicsDevice.BindConstantBuffer(BackingBuffer, Slot);
    }
}