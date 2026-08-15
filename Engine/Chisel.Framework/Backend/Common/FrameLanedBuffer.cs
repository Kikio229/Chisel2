using System;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

internal sealed class FrameLanedBuffer : IDisposable
{
    IGraphicsDevice _device;
    IBuffer[] _buffers;
    byte[] _shadow;
    (int min, int max)[] _dirty;
    bool _disposed;

    public FrameLanedBuffer(IGraphicsDevice device, ulong size, BufferType type, BufferUsage usage)
    {
        _device = device;
        _shadow = new byte[size];
        _buffers = new IBuffer[device.BufferingCount];
        _dirty = new (int, int)[device.BufferingCount];

        for (int i = 0; i < _buffers.Length; i++)
        {
            _buffers[i] = device.CreateBuffer(new BufferDescription { Size = size, Type = type, Usage = usage });
            _dirty[i] = (int.MaxValue, int.MinValue); // fresh buffer, nothing to flush yet
        }
    }

    // Writes into the CPU shadow only. Marks every lane's dirty
    // range so each lane knows it owes an upload before it's next bound.
    public void Write(ReadOnlySpan<byte> data, int offset)
    {
        data.CopyTo(new Span<byte>(_shadow, offset, data.Length));

        for (int i = 0; i < _dirty.Length; i++)
        {
            _dirty[i].min = Math.Min(_dirty[i].min, offset);
            _dirty[i].max = Math.Max(_dirty[i].max, offset + data.Length);
        }
    }

    // Call immediately before binding/using this frame's lane. Uploads only the byte
    // range that's actually changed since this specific lane was last flushed.
    public IBuffer FlushBeforeBind()
    {
        uint lane = _device.FrameIndex;
        var (min, max) = _dirty[lane];

        if (min <= max)
        {
            _device.UpdateBuffer(_buffers[lane], new ReadOnlySpan<byte>(_shadow, min, max - min), (ulong)min);
            _dirty[lane] = (int.MaxValue, int.MinValue);
        }

        return _buffers[lane];
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var b in _buffers)
        {
            if (b is IDisposable d) d.Dispose();
        }

        _disposed = true;
    }
}