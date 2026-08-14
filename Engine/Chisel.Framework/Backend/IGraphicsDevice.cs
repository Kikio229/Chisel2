using System;
using Microsoft.Xna.Framework; // TODO: Change the XNA namespace
using Chisel.Resource;

namespace Chisel.Framework;

public interface IGraphicsDevice
{
    GraphicsBackend Backend { get; }
    ImageFormat[] CurrentColorFormats { get; }
    ImageFormat? CurrentDepthStencilFormat { get; }
    uint CurrentSampleCount { get; }

    void BeginFrame();
    void EndFrame();
    void BeginDrawing(IRenderTarget target);
    void EndDrawing();
    void Clear(Color clearColor);
    void Clear(GraphicsClearFlags flags, Color clearColor, float clearDepth, int clearStencil);
    void Resize(int width, int height);
    void Draw(uint vtxCount);
    void DrawIndexed(uint idxCount);
    void DrawIndexed(uint idxCount, uint startIndex, int baseVertex);
    void DrawInstanced(uint vtxCount, uint instCount);
    void DrawIndexedInstanced(uint idxCount, uint instCount);
    void DrawIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride);
    void DrawIndexedIndirect(IBuffer buffer, ulong offset, uint drawCount, uint stride);
    void Dispatch(uint groupX, uint groupY, uint groupZ);
    void DispatchIndirect(IBuffer buffer, ulong offset);

    void SetViewport(Vector2 position, Vector2 size);
    void SetScissor(Vector2 position, Vector2 size);
    void SetScissorEnabled(bool enabled);
    void SetConstants<T>(in T value, uint slot) where T : unmanaged;
    void SetVertexLayout(VertexLayoutDescription layout, uint slot);

    void BindVertexBuffer(IBuffer buffer, uint slot);
    void BindIndexBuffer(IBuffer buffer);
    void BindConstantBuffer(IBuffer buffer, uint slot);
    void BindStorageBuffer(IBuffer buffer);
    void BindImage(IImage image, uint slot);
    void BindSampler(ISampler sampler, uint slot);
    void BindGraphicsState(IGraphicsState gfxState);
    void BindComputeState(IComputeState cmpState);
    void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset);
    void CopyBuffer(IBuffer bufSrc, IBuffer bufDst);
    void CopyBufferToImage(IBuffer bufSrc, IImage imgDst);
    void ResolveImage(IImage src, IImage dst); // For MSAA
    void CopyImage(IImage imgSrc, IImage imgDst);
    void CopyImageToBuffer(IImage imgSrc, IBuffer bufDst);
    void GenerateMips(IImage image, ReadOnlySpan<byte> baseLevelData);

    IBuffer CreateBuffer(BufferDescription bufDesc);
    IImage CreateImage(ImageDescription imgDesc);
    ISampler CreateSampler(SamplerDescription smpDesc);
    IShader CreateShader(ShaderDescription shdDesc);
    IRenderTarget CreateRenderTarget(RenderTargetDescription renDesc);
    IGraphicsState CreateGraphicsState(GraphicsStateDescription gfxDesc);
    IComputeState CreateComputeState(ComputeStateDescription cmpDesc);
}