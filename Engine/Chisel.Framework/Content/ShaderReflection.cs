using System;
using System.Collections.Generic;
using System.Text;

namespace Chisel.Resource;

public class ShaderReflection
{
    public ConstantBufferReflection[] ConstantBuffers;
    public ResourceReflection[] Images;
    public ResourceReflection[] Samplers;
    public VertexInputReflection[] Inputs;
}
public class VertexInputReflection
{
    public string SemanticName;
    public uint SemanticIndex;
}
public class ConstantBufferReflection
{
    public string Name;
    public uint Slot;
    public int SizeInBytes;
    public ConstantBufferMemberReflection[] Members;
}

public class ConstantBufferMemberReflection
{
    public string Name;
    public int Offset;
    public int SizeInBytes;
}

public class ResourceReflection
{
    public string Name;
    public uint Slot;
}