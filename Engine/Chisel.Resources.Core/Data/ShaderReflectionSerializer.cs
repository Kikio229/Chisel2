using System.IO;

namespace Chisel.Resource;

public static class ShaderReflectionSerializer
{
    public static void Write(BinaryWriter writer, ShaderReflection reflection)
    {
        writer.Write(reflection.ConstantBuffers.Length);
        foreach (ConstantBufferReflection cbuffer in reflection.ConstantBuffers)
        {
            writer.Write(cbuffer.Name);
            writer.Write(cbuffer.Slot);
            writer.Write(cbuffer.SizeInBytes);
            writer.Write(cbuffer.Members.Length);

            foreach (ConstantBufferMemberReflection member in cbuffer.Members)
            {
                writer.Write(member.Name);
                writer.Write(member.Offset);
                writer.Write(member.SizeInBytes);
            }
        }

        writer.Write(reflection.Images.Length);
        foreach (ResourceReflection image in reflection.Images)
        {
            writer.Write(image.Name);
            writer.Write(image.Slot);
        }

        writer.Write(reflection.Samplers.Length);
        foreach (ResourceReflection sampler in reflection.Samplers)
        {
            writer.Write(sampler.Name);
            writer.Write(sampler.Slot);
        }

        writer.Write(reflection.Inputs?.Length ?? 0);
        if (reflection.Inputs != null)
        {
            foreach (VertexInputReflection input in reflection.Inputs)
            {
                writer.Write(input.SemanticName);
                writer.Write(input.SemanticIndex);
            }
        }
    }

    public static ShaderReflection Read(BinaryReader reader)
    {
        int constantBufferCount = reader.ReadInt32();
        ConstantBufferReflection[] constantBuffers = new ConstantBufferReflection[constantBufferCount];

        for (int i = 0; i < constantBufferCount; i++)
        {
            string name = reader.ReadString();
            uint slot = reader.ReadUInt32();
            int sizeInBytes = reader.ReadInt32();
            int memberCount = reader.ReadInt32();
            ConstantBufferMemberReflection[] members = new ConstantBufferMemberReflection[memberCount];

            for (int j = 0; j < memberCount; j++)
            {
                members[j] = new ConstantBufferMemberReflection
                {
                    Name = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    SizeInBytes = reader.ReadInt32(),
                };
            }

            constantBuffers[i] = new ConstantBufferReflection
            {
                Name = name,
                Slot = slot,
                SizeInBytes = sizeInBytes,
                Members = members,
            };
        }

        int imageCount = reader.ReadInt32();
        ResourceReflection[] images = new ResourceReflection[imageCount];
        for (int i = 0; i < imageCount; i++)
        {
            images[i] = new ResourceReflection { Name = reader.ReadString(), Slot = reader.ReadUInt32() };
        }

        int samplerCount = reader.ReadInt32();
        ResourceReflection[] samplers = new ResourceReflection[samplerCount];
        for (int i = 0; i < samplerCount; i++)
        {
            samplers[i] = new ResourceReflection { Name = reader.ReadString(), Slot = reader.ReadUInt32() };
        }

        int inputCount = reader.ReadInt32();
        VertexInputReflection[] inputs = new VertexInputReflection[inputCount];

        for (int i = 0; i < inputCount; i++)
        {
            inputs[i] = new VertexInputReflection
            {
                SemanticName = reader.ReadString(),
                SemanticIndex = reader.ReadUInt32(),
            };
        }
        
        return new ShaderReflection
        {
            ConstantBuffers = constantBuffers,
            Images = images,
            Samplers = samplers,
            Inputs = inputs
        };
    }
}