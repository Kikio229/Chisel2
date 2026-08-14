using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;
class D3DShaderCompiler : IShaderCompiler
{
    public GraphicsBackend Backend => GraphicsBackend.Direct3D12;

    public (byte[] Bytecode, ShaderReflection Reflection) Compile(string source, string entry, ShaderStage stage)
    {
        string profile = TranslateProfile(stage);
        byte[] dxil = DxcHelper.Compile(source, entry, profile, spirv: false);
        ShaderReflection reflection = DxcHelper.ReflectDxil(dxil, stage);
        return (dxil, reflection);
    }

    static string TranslateProfile(ShaderStage stage)
    {
        if ((stage & ShaderStage.Vertex) != 0)
        {
            return "vs_6_0";
        }
        if ((stage & ShaderStage.Pixel) != 0)
        {
            return "ps_6_0";
        }
        if ((stage & ShaderStage.Compute) != 0)
        {
            return "cs_6_0";
        }
        throw new ArgumentOutOfRangeException(nameof(stage));
    }
}