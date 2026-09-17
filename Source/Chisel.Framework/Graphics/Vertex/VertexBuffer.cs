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
    FrameLanedBuffer buffer;
    bool disposedValue;

    public VertexBuffer(IGraphicsDevice device, int capacity)
    {
        this.device = device;
        Layout = VertexLayoutCache.Get<T>();
        Count = capacity;
        buffer = new FrameLanedBuffer(device, (ulong)(capacity * Layout.Stride), BufferType.Upload, BufferUsage.Vertex);
    }

    public void SetData(ReadOnlySpan<T> data)
    {
        buffer.Write(MemoryMarshal.AsBytes(data), 0);
        Count = data.Length;
    }

    public void SetData(ReadOnlySpan<T> data, int startVertex)
    {
        buffer.Write(MemoryMarshal.AsBytes(data), startVertex * Marshal.SizeOf<T>());
        Count = Math.Max(Count, startVertex + data.Length);
    }

    public void Bind(uint slot)
    {
        device.BindVertexBuffer(buffer.FlushBeforeBind(), slot);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) buffer.Dispose();
            disposedValue = true;
        }
    }

    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}