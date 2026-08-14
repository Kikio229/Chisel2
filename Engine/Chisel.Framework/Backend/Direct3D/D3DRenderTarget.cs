using System;
using Vortice.Win32.Graphics.Direct3D12;

namespace Chisel.Framework;

internal class D3DRenderTarget : Disposable, IRenderTarget
{
    public IImage[]? Color { get; }
    public IImage? DepthStencil { get; }
    internal D3DImage[]? ColorImages { get; }
    internal D3DImage? DepthStencilImage { get; }

    internal uint Width { get; }
    internal uint Height { get; }
    internal CpuDescriptorHandle[]? RtvHandles { get; }
    internal CpuDescriptorHandle? DsvHandle { get; }

    private D3DDescriptorHeap? _rtvHeap;
    private D3DDescriptorHeap? _dsvHeap;

    public unsafe D3DRenderTarget(ID3D12Device* device, D3DImage[]? color, D3DImage? depthStencil)
    {
        Color = color;
        DepthStencil = depthStencil;
        ColorImages = color;
        DepthStencilImage = depthStencil;

        Width = ColorImages is { Length: > 0 } ? ColorImages[0].Width : DepthStencilImage!.Width;
        Height = ColorImages is { Length: > 0 } ? ColorImages[0].Height : DepthStencilImage!.Height;

        if (ColorImages is { Length: > 0 })
        {
            _rtvHeap = new D3DDescriptorHeap(device, DescriptorHeapType.Rtv, (uint)ColorImages.Length, shaderVisible: false);
            CpuDescriptorHandle[] handles = new CpuDescriptorHandle[ColorImages.Length];

            for (int i = 0; i < ColorImages.Length; i++)
            {
                bool multisampled = ColorImages[i].SampleCount > 1;

                RenderTargetViewDescription rtvDesc = new RenderTargetViewDescription
                {
                    Format = D3DUtilities.GetDxgiFormatFromImage(ColorImages[i].Format),
                    ViewDimension = multisampled ? RtvDimension.Texture2DMs : RtvDimension.Texture2D,
                };

                CpuDescriptorHandle handle = _rtvHeap.GetCpuAt((uint)i);
                device->CreateRenderTargetView(ColorImages[i].Resource, &rtvDesc, handle);
                handles[i] = handle;
            }

            RtvHandles = handles;
        }

        if (DepthStencilImage != null)
        {
            _dsvHeap = new D3DDescriptorHeap(device, DescriptorHeapType.Dsv, 1, shaderVisible: false);
            bool multisampled = DepthStencilImage.SampleCount > 1;

            DepthStencilViewDescription dsvDesc = new DepthStencilViewDescription
            {
                Format = D3DUtilities.GetDxgiFormatFromImage(DepthStencilImage.Format),
                ViewDimension = multisampled ? DsvDimension.Texture2DMs : DsvDimension.Texture2D,
                Flags = DsvFlags.None,
            };

            CpuDescriptorHandle handle = _dsvHeap.GetCpuAt(0);
            device->CreateDepthStencilView(DepthStencilImage.Resource, &dsvDesc, handle);
            DsvHandle = handle;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ColorImages != null)
            {
                foreach (var c in ColorImages)
                {
                    c.Dispose();
                }
            }

            DepthStencilImage?.Dispose();
            _rtvHeap?.Dispose();
            _dsvHeap?.Dispose();
        }
    }
}
