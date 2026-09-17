using Chisel.Framework;

namespace Chisel.Resource.Builder;

interface IShaderCompiler
{
    GraphicsBackend Backend { get; }
    (byte[] Bytecode, ShaderReflection Reflection) Compile(string source, string entry, ShaderStage stage);
}