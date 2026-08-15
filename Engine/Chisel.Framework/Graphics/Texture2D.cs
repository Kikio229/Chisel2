using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class Texture2D : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public ImageFormat Format { get; }
    public uint MipLevels { get; }
    public IImage Image { get; }

    IGraphicsDevice device;
    IBuffer stagingBuffer;
    ulong stagingBufferSize;
    bool disposedValue;

    public Texture2D(IGraphicsDevice device, int width, int height, ImageFormat format = ImageFormat.R8G8B8A8UNorm, ImageUsage usage = ImageUsage.Sampled, uint samples = 1, bool generateMips = false)
    {
        this.device = device;
        Width = width;
        Height = height;
        Format = format;
        MipLevels = generateMips ? CalculateMipLevels(width, height) : 1;

        Image = device.CreateImage(new ImageDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = MipLevels,
            SampleCount = samples,
            Format = format,
            Usage = usage,
        });
    }
    public static uint CalculateMipLevels(int width, int height)
    {
        return (uint)MathF.Floor(MathF.Log2(MathF.Max(width, height))) + 1;
    }
    public void SetData(ReadOnlySpan<byte> data)
    {
        EnsureStagingBuffer((ulong)data.Length);

        device.UpdateBuffer(stagingBuffer, data, 0);
        device.CopyBufferToImage(stagingBuffer, Image);

        Logger.AppendLog("Texture2D", $"SetData: {Width}x{Height}, MipLevels={MipLevels}", ConsoleColor.Yellow, 1);

        if (MipLevels > 1)
        {
            try
            {
                device.GenerateMipmaps(Image, data);
                Logger.AppendLog("Texture2D", "GenerateMips completed successfully.", ConsoleColor.Yellow, 1);
            }
            catch (Exception ex)
            {
                Logger.AppendLog("Texture2D", $"GenerateMips THREW: {ex}", ConsoleColor.Red, 1);
                throw; // don't swallow it here even if something upstream might
            }
        }
    }
    public void SetData(int x, int y, int width, int height, ReadOnlySpan<byte> data)
    {
        EnsureStagingBuffer((ulong)data.Length);
        device.UpdateBuffer(stagingBuffer, data, 0);
        device.CopyBufferToImage(stagingBuffer, Image, new ImageBufferCopyRegion
        {
            Width = (uint)width,
            Height = (uint)height,
            BuffOffset = 0,
            DstOffsetX = x,
            DstOffsetY = y,
            ImgMipLevel = 0,
        });
    }
    void EnsureStagingBuffer(ulong size)
    {
        // Reused across calls, not recreated - destroying a buffer right after using it as a
        // PBO transfer source forces the driver to stall until that (possibly still in-flight)
        // transfer completes. Only reallocated if a caller ever asks for more space than
        // currently held - in practice this never happens after the first call, since a given
        // Texture2D's dimensions (and therefore the byte size CopyBufferToImage transfers) are
        // fixed for its whole lifetime.
        if (stagingBuffer == null || stagingBufferSize < size)
        {
            if (stagingBuffer is IDisposable disposableStaging)
            {
                disposableStaging.Dispose();
            }

            stagingBuffer = device.CreateBuffer(new BufferDescription
            {
                Size = size,
                Type = BufferType.Upload,
                Usage = BufferUsage.CopySource,
            });
            stagingBufferSize = size;
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (Image is IDisposable disposableImage)
                {
                    disposableImage.Dispose();
                }

                if (stagingBuffer is IDisposable disposableStaging)
                {
                    disposableStaging.Dispose();
                }
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