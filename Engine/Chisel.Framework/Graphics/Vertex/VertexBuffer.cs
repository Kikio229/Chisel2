using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class VertexBuffer<T> : IDisposable where T : unmanaged
{
    public VertexLayoutDescription Layout { get; }
    public int Count { get; private set; }

    IGraphicsDevice device;
    IBuffer buffer;
    bool disposedValue;

    public VertexBuffer(IGraphicsDevice device, int capacity)
    {
        this.device = device;
        Layout = VertexLayoutCache.Get<T>();
        Count = capacity;

        buffer = device.CreateBuffer(new BufferDescription
        {
            Size = (ulong)(capacity * Layout.Stride),
            Type = BufferType.Upload,
            Usage = BufferUsage.Vertex,
        });
    }

    public void SetData(ReadOnlySpan<T> data)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data);
        device.UpdateBuffer(buffer, bytes, 0);
        Count = data.Length;
    }
    public void SetData(ReadOnlySpan<T> data, int startVertex)
    {
        device.UpdateBuffer(buffer, MemoryMarshal.AsBytes(data), (ulong)(startVertex * Marshal.SizeOf<T>()));
        Count = Math.Max(Count, startVertex + data.Length);
    }
    public void Bind(uint slot)
    {
        device.BindVertexBuffer(buffer, slot);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing && buffer is IDisposable disposableBuffer)
            {
                disposableBuffer.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}