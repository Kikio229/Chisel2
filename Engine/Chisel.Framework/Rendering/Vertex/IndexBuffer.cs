using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class IndexBuffer : IDisposable
{
    public int Count { get; private set; }

    IGraphicsDevice device;
    IBuffer buffer;
    bool disposedValue;

    public IndexBuffer(IGraphicsDevice device, int capacity)
    {
        this.device = device;
        buffer = device.CreateBuffer(new BufferDescription
        {
            Size = (ulong)(capacity * sizeof(uint)),
            Type = BufferType.Upload,
            Usage = BufferUsage.Index,
        });
    }

    public void SetData(ReadOnlySpan<uint> data)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data);
        device.UpdateBuffer(buffer, bytes, 0);
        Count = data.Length;
    }

    public void Bind()
    {
        device.BindIndexBuffer(buffer);
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