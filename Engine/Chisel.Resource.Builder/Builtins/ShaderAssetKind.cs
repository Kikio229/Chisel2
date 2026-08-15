using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;
public class ShaderAssetKind : IBuiltInAssetKind
{
    public string Label => "shader";
    public string ResourcePrefix => "Chisel.Resource.Builder.ShaderLibrary.BuiltIn.";
    public string ResourceSuffix => ".hlsl";

    public byte[] Resolve(string name, byte[] embeddedData)
    {
        string text = Encoding.UTF8.GetString(embeddedData);
        string source = ShaderPreprocessor.ExpandLibraries(text);
        ShaderAssetHandler.DumpPreprocessedIfRequested(name, source);
        ShaderAssetHandler shaderHandler = (ShaderAssetHandler)AssetHandlerRegistry.Resolve(AssetType.Shader);
        return shaderHandler.CompileSource(source);
    }
    public string GetOutputPath(string name)
    {
        return ContentPath.Normalize("Shaders/" + name + "." + ShaderContentInfo.FileExtension);
    }
}