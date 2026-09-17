using System;
using System.IO;

namespace Chisel.Framework;

public static class ShaderReflectionSerializer
{
    public static ShaderReflection Read(BinaryReader reader)
    {
        int CbufferCount = reader.ReadInt32();
        CbufferReflection[] Cbuffers = new CbufferReflection[CbufferCount];

        for (int i = 0; i < CbufferCount; i++)
        {
            string name = reader.ReadString();
            uint slot = reader.ReadUInt32();
            int sizeInBytes = reader.ReadInt32();
            int memberCount = reader.ReadInt32();
            MemberReflection[] members = new MemberReflection[memberCount];

            for (int j = 0; j < memberCount; j++)
            {
                members[j] = new MemberReflection
                {
                    Name = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    SizeInBytes = reader.ReadInt32(),
                };
            }

            Cbuffers[i] = new CbufferReflection
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
            images[i] = new ResourceReflection { Name = reader.ReadString(), CompiledName = reader.ReadString(), Slot = reader.ReadUInt32() };
        }

        int samplerCount = reader.ReadInt32();
        ResourceReflection[] samplers = new ResourceReflection[samplerCount];

        for (int i = 0; i < samplerCount; i++)
        {
            samplers[i] = new ResourceReflection { Name = reader.ReadString(), CompiledName = reader.ReadString(), Slot = reader.ReadUInt32() };
        }

        int inputCount = reader.ReadInt32();
        InputReflection[] inputs = new InputReflection[inputCount];

        for (int i = 0; i < inputCount; i++)
        {
            inputs[i] = new InputReflection
            {
                Name = reader.ReadString(),
                Index = reader.ReadUInt32(),
            };
        }

        return new ShaderReflection
        {
            Images = images,
            Samplers = samplers,
            Cbuffers = Cbuffers,
            Inputs = inputs
        };
    }

    public static void Write(BinaryWriter writer, ShaderReflection reflection)
    {
        writer.Write(reflection.Cbuffers.Length);

        foreach (CbufferReflection c in reflection.Cbuffers)
        {
            writer.Write(c.Name);
            writer.Write(c.Slot);
            writer.Write(c.SizeInBytes);
            writer.Write(c.Members.Length);

            foreach (MemberReflection m in c.Members)
            {
                writer.Write(m.Name);
                writer.Write(m.Offset);
                writer.Write(m.SizeInBytes);
            }
        }

        writer.Write(reflection.Images.Length);

        foreach (ResourceReflection i in reflection.Images)
        {
            writer.Write(i.Name);
            writer.Write(i.CompiledName);
            writer.Write(i.Slot);
        }

        writer.Write(reflection.Samplers.Length);

        foreach (ResourceReflection s in reflection.Samplers)
        {
            writer.Write(s.Name);
            writer.Write(s.CompiledName);
            writer.Write(s.Slot);
        }

        writer.Write(reflection.Inputs?.Length ?? 0);

        if (reflection.Inputs != null)
        {
            foreach (InputReflection i in reflection.Inputs)
            {
                writer.Write(i.Name);
                writer.Write(i.Index);
            }
        }
    }
}