using System;
using Chisel.Framework;

namespace Chisel.Resource.Builder;
public class TextureAssetKind : IBuiltInAssetKind
{
    public string Label => "texture";
    public string ResourcePrefix => "Chisel.Resource.Builder.TextureLibrary.";
    public string ResourceSuffix => ".png";
    public byte[] Resolve(string name, byte[] embeddedData)
    {
        return embeddedData;
    }
    public string GetOutputPath(string name)
    {
        return ContentPath.Normalize("Textures/" + name + ".png");
    }
}
