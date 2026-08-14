using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class TextureContentLoader : IContentLoader<Texture2D>
{
    public string[] Extensions => new[] { "png", "jpg", "jpeg", "bmp", "tga" };
    IGraphicsDevice device;

    public TextureContentLoader(IGraphicsDevice device)
    {
        this.device = device;
    }

    public Texture2D Load(Stream stream)
    {
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        Texture2D texture = new Texture2D(device, image.Width, image.Height, ImageFormat.R8G8B8A8UNorm,generateMips:true);
        texture.SetData(image.Data);

        return texture;
    }
}