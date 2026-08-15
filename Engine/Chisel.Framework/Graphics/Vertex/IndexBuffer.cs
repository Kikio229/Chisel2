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
    FrameLanedBuffer buffer;
    bool disposedValue;

    public IndexBuffer(IGraphicsDevice device, int capacity)
    {
        this.device = device;
        buffer = new FrameLanedBuffer(device, (ulong)(capacity * sizeof(uint)), BufferType.Upload, BufferUsage.Index);
    }

    public void SetData(ReadOnlySpan<uint> data)
    {
        buffer.Write(MemoryMarshal.AsBytes(data), 0);
        Count = data.Length;
    }

    public void SetData(ReadOnlySpan<uint> data, int startIndex)
    {
        buffer.Write(MemoryMarshal.AsBytes(data), sizeof(uint)*startIndex);
        Count = data.Length;
    }

    public void Bind()
    {
        device.BindIndexBuffer(buffer.FlushBeforeBind());
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