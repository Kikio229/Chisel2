using System;
using System.Collections.Generic;
using System.Text;

namespace Chisel.Resource;
public enum GraphicsBackend
{
    Auto,
    Direct3D12,
    OpenGL,
}
public struct ShaderVariantEntry
{
    public string Technique;
    public GraphicsBackend Backend;
    public ShaderStage Stage;
    public string Entry;
    public int BytecodeOffset;
    public int BytecodeLength;
    public int ReflectionOffset;
    public int ReflectionLength;
}
public static class ShaderContentInfo
{
    public const string FileExtension = "csl";
}