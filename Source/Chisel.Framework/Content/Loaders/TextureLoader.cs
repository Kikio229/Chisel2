using System;
using System.IO;
using StbImageSharp;

namespace Chisel.Framework;

public class TextureLoader : ILoader<Texture2D>
{
    public string[] Extensions { get; }

    private IGraphicsDevice _device;

    public TextureLoader(IGraphicsDevice device)
    {
        Extensions = new string[5] 
        { 
            "png", 
            "jpg", 
            "jpeg", 
            "bmp", 
            "tga" 
        };

        _device = device;
    }

    public Texture2D Load(Stream stream)
    {
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        Texture2D texture = new Texture2D(_device, image.Width, image.Height, ImageFormat.R8G8B8A8UNorm, generateMips: true);
        texture.SetData(image.Data);
        return texture;
    }
}