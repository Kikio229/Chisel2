using FontStashSharp.Interfaces;
using System;
using System.Drawing;

namespace Chisel.Framework.UI;

public class FSSTextureManager : ITexture2DManager
{
    public object CreateTexture(int width, int height)
    {
        return new Texture2D(Game.Instance?.GraphicsDevice ?? throw new Exception("C2 Game not initialized before using fonts!"), width, height);
    }

    public System.Drawing.Point GetTextureSize(object texture)
    {
        if (texture is not Texture2D cTexture) throw new Exception("FSSTextureManager was passed an invalid texture!");

        return new System.Drawing.Point(cTexture.Width,cTexture.Height);
    }

    public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
    {
        if (texture is not Texture2D cTexture) throw new Exception("FSSTextureManager was passed an invalid texture!");

        cTexture.SetData(bounds.X, bounds.Y, bounds.Width, bounds.Height, data);
    }
}

public class FFRenderer2(SpriteBatch sb) : IFontStashRenderer2
{
    public ITexture2DManager TextureManager { get; } = new FSSTextureManager();
    SpriteBatch spriteBatch = sb;

    public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
    {
        if (texture is not Texture2D cTexture)
        {
            throw new Exception("FFRenderer2 was passed an invalid texture!");
        }

        spriteBatch.DrawQuad(cTexture,
            ToSpriteVertex(topLeft),
            ToSpriteVertex(topRight),
            ToSpriteVertex(bottomRight),
            ToSpriteVertex(bottomLeft));
    }
    static SpriteVertex ToSpriteVertex(VertexPositionColorTexture v)
    {
        return new SpriteVertex
        {
            Position = new Vector3(v.Position.X, v.Position.Y, v.Position.Z),
            UV = new Vector2(v.TextureCoordinate.X, v.TextureCoordinate.Y),
            Color = new Vector4(v.Color.R / 255f, v.Color.G / 255f, v.Color.B / 255f, v.Color.A / 255f),
        };
    }
}