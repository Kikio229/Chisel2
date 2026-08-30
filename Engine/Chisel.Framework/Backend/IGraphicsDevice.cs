using System;

namespace Chisel.Framework;

public interface IGraphicsDevice
{
    uint FrameIndex { get; }
    uint SampleCount { get; }
    uint BufferCount { get; }
    GraphicsBackend Backend { get; }
    ImageFormat[] ColorFormats { get; }
    ImageFormat? DepthStencilFormat { get; }

    void BeginFrame();
    void EndFrame();
    void BeginDrawing(IRenderTarget target);
    void EndDrawing();
    void Clear(Color clearColor);
    void Clear(Color clearColor, float clearDepth, int clearStencil, GraphicsClearFlags flags);
    void Resize(int width, int height);

    void Draw(uint vertexCount);
    void DrawIndexed(uint indexCount);
    void DrawIndexed(uint indexCount, uint startIndex, int baseVertex);
    void DrawInstanced(uint vertexCount, uint instCount);
    void DrawIndexedInstanced(uint indexCount, uint instCount);
    void DrawIndexedInstanced(uint indexCount, uint instCount, uint startIndex, int baseVertex);
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
    void BindConstantBuffer(IBuffer buffer, ulong offset, uint size, uint slot);
    void BindStorageBuffer(IBuffer buffer);
    void BindImage(IImage image, uint slot);
    void BindSampler(ISampler sampler, uint slot);
    void BindGraphicsState(IGraphicsState graphicsState);
    void BindComputeState(IComputeState computeState);
    void BindMaterialTable(IMaterialTable materialTable);

    void UpdateBuffer(IBuffer buffer, ReadOnlySpan<byte> data, ulong offset);
    (IBuffer arena, ulong offset) SuballocateBuffer(ReadOnlySpan<byte> data);
    void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst);
    void CopyBuffer(IBuffer bufferSrc, IBuffer bufferDst, BufferCopyRegion region);
    void CopyBufferToImage(IBuffer buffer, IImage image);
    void CopyBufferToImage(IBuffer buffer, IImage image, ImageBufferCopyRegion region);

    void ResolveImage(IImage imageSrc, IImage imageDst); // For MSAA
    void CopyImage(IImage imageSrc, IImage imageDst, ImageCopyRegion region);
    void CopyImage(IImage imageSrc, IImage imageDst);
    void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst);
    void CopyImageToBuffer(IImage imageSrc, IBuffer bufferDst, ImageBufferCopyRegion region);

    IBuffer CreateBuffer(BufferDescription bufferDesc);
    IImage CreateImage(ImageDescription imageDesc);
    ISampler CreateSampler(SamplerDescription samplerDesc);
    IShader CreateShader(ShaderDescription shaderDesc);
    IRenderTarget CreateRenderTarget(RenderTargetDescription targetDesc);
    IGraphicsState CreateGraphicsState(GraphicsStateDescription graphicsDesc);
    IComputeState CreateComputeState(ComputeStateDescription computeDesc);
    IMaterialTable CreateMaterialTable(MaterialTableDescription materialDesc);
    void GenerateMipmaps(IImage image, ReadOnlySpan<byte> baseData);
}