using Chisel.Resource;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Resource.Builder;

unsafe class GLShaderCompiler : IShaderCompiler
{
    static Cross api = Cross.GetApi();

    public GraphicsBackend Backend => GraphicsBackend.OpenGL46;

    const bool DumpShaders = false;

    public (byte[] Bytecode, ShaderReflection Reflection) Compile(string source, string entry, ShaderStage stage)
    {
        string profile = TranslateProfile(stage);
        byte[] spirv = DxcHelper.Compile(source, entry, profile, spirv: true);

        Context* context;
        api.ContextCreate(&context);

        ParsedIr* ir;
        fixed (byte* spirvPtr = spirv)
        {
            api.ContextParseSpirv(context, (uint*)spirvPtr, (nuint)(spirv.Length / 4), &ir);
        }
        Silk.NET.SPIRV.Cross.Compiler* compiler;
        api.ContextCreateCompiler(context, Silk.NET.SPIRV.Cross.Backend.Glsl, ir, CaptureMode.TakeOwnership, &compiler);

        CompilerOptions* options;
        api.CompilerCreateCompilerOptions(compiler, &options);
        api.CompilerOptionsSetUint(options, CompilerOption.GlslVersion, 330);
        api.CompilerOptionsSetBool(options, CompilerOption.GlslES, 0);
        api.CompilerOptionsSetBool(options, CompilerOption.GlslSeparateShaderObjects, 1);
        api.CompilerInstallCompilerOptions(compiler, options);

        api.CompilerBuildCombinedImageSamplers(compiler);

        byte* glslSource;
        Result compileResult = api.CompilerCompile(compiler, &glslSource);

        if (compileResult != Result.Success)
        {
            byte* errorMessage = api.ContextGetLastErrorString(context);
            string error = errorMessage != null ? new string((sbyte*)errorMessage) : "Unknown SPIRV-Cross error";
            api.ContextDestroy(context);
            throw new InvalidOperationException("SPIRV-Cross failed to compile GLSL for '" + entry + "': " + error);
        }

        string glslText = new string((sbyte*)glslSource);
        byte[] glslBytes = System.Text.Encoding.UTF8.GetBytes(new string((sbyte*)glslSource));

        if(DumpShaders)
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "GLSLDebug");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, Guid.NewGuid().ToString() + entry + "_" + stage + ".glsl");
            File.WriteAllText(path, glslText);
        }

        ShaderReflection reflection = ReflectSpirv(compiler);

        api.ContextDestroy(context);

        return (glslBytes, reflection);
    }
    ShaderReflection ReflectSpirv(Silk.NET.SPIRV.Cross.Compiler* compiler)
    {
        Silk.NET.SPIRV.Cross.Resources* resources;
        api.CompilerCreateShaderResources(compiler, &resources);

        List<ConstantBufferReflection> constantBuffers = new List<ConstantBufferReflection>();
        List<ResourceReflection> images = new List<ResourceReflection>();
        List<ResourceReflection> samplers = new List<ResourceReflection>();

        ReflectedResource* uniformBuffers;
        nuint uniformBufferCount;
        api.ResourcesGetResourceListForType(resources, ResourceType.UniformBuffer, &uniformBuffers, &uniformBufferCount);

        for (uint i = 0; i < uniformBufferCount; i++)
        {
            ReflectedResource resource = uniformBuffers[i];
            uint slot = api.CompilerGetDecoration(compiler, resource.Id, Decoration.Binding);
            uint typeId = resource.BaseTypeId;
            Silk.NET.SPIRV.Cross.CrossType* structType = api.CompilerGetTypeHandle(compiler, typeId);
            uint memberCount = api.TypeGetNumMemberTypes(structType);

            nuint structSize;
            api.CompilerGetDeclaredStructSize(compiler, structType, &structSize);

            List<ConstantBufferMemberReflection> members = new List<ConstantBufferMemberReflection>();

            for (uint m = 0; m < memberCount; m++)
            {
                byte* memberName = api.CompilerGetMemberName(compiler, typeId, m);
                uint offset;
                api.CompilerTypeStructMemberOffset(compiler, structType, m, &offset);

                members.Add(new ConstantBufferMemberReflection
                {
                    Name = new string((sbyte*)memberName),
                    Offset = (int)offset,
                });
            }

            constantBuffers.Add(new ConstantBufferReflection
            {
                Name = new string((sbyte*)resource.Name),
                Slot = slot,
                SizeInBytes = (int)structSize,
                Members = members.ToArray(),
            });
        }

        CombinedImageSampler* combinedSamplers;
        nuint combinedCount;
        api.CompilerGetCombinedImageSamplers(compiler, &combinedSamplers, &combinedCount);

        for (nuint i = 0; i < combinedCount; i++)
        {
            CombinedImageSampler combined = combinedSamplers[i];

            uint slot = api.CompilerGetDecoration(compiler, combined.CombinedId, Decoration.Binding);
            byte* imageName = api.CompilerGetName(compiler, combined.ImageId);
            byte* samplerName = api.CompilerGetName(compiler, combined.SamplerId);

            images.Add(new ResourceReflection { Name = new string((sbyte*)imageName), Slot = slot });
            samplers.Add(new ResourceReflection { Name = new string((sbyte*)samplerName), Slot = slot });
        }

        return new ShaderReflection
        {
            ConstantBuffers = constantBuffers.ToArray(),
            Images = images.ToArray(),
            Samplers = samplers.ToArray(),
        };
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
        throw new ArgumentOutOfRangeException(nameof(stage));
    }
}