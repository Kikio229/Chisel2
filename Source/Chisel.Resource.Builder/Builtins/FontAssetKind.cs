using System;
using Chisel.Framework;

namespace Chisel.Resource.Builder;
public class FontAssetKind : IBuiltInAssetKind
{
    public string Label => "font";
    public string ResourcePrefix => "Chisel.Resource.Builder.FontLibrary.";
    public string ResourceSuffix => ".ttf";
    public byte[] Resolve(string name, byte[] embeddedData)
    {
        return embeddedData;
    }
    public string GetOutputPath(string name)
    {
        return ContentPath.Normalize("Fonts/" + name + ".ttf");
    }
}
