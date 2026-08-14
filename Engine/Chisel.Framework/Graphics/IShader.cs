using Chisel.Resource;
using System;

namespace Chisel.Framework;

public interface IShader
{
    string Entry { get; }
    ShaderStage Stage { get; }
    ShaderReflection Reflection { get; }
}
