using System;

namespace Chisel.Framework;

public struct ShaderReflection
{
    public ResourceReflection[] Images;
    public ResourceReflection[] Samplers;
    public CbufferReflection[] Cbuffers;
    public InputReflection[] Inputs;

    public ShaderReflection()
    {
        Images = Array.Empty<ResourceReflection>();
        Samplers = Array.Empty<ResourceReflection>();
        Cbuffers = Array.Empty<CbufferReflection>();
        Inputs = Array.Empty<InputReflection>();
    }
}
