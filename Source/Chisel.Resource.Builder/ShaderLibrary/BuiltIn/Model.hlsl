// ============================================================================
// Model.hlsl
// Standard lit model shader: diffuse + specular + normal map, static lights,
// realtime lights, SH ambient, cubemap reflections, fog.
//
// One pixel shader shared by both techniques -- "Default" (static meshes) and
// "Skinned" (GPU-skinned meshes) only differ in the vertex shader, and
// VSSkinned's whole job is to skin the vertex and then hand off to VSMain.
//
// Load with: Content.Load<ShaderEffect>("Shaders/Model")
//   shader.SetTechnique("Default");   // or "Skinned"
// ============================================================================
#pragma pack_matrix(row_major)

#library Common
#library Fog
#library StaticLights  // pulls in DynamicLights + Sun too
#library AmbientProbe
#library Cubemap
#library Skinning

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
    float4x4 WorldInverseTranspose;
};

// b11
cbuffer MaterialConstants : register(b11)
{
    float Shininess;
    int   Transparent; // int, not bool: C# `bool` is 1 byte, an HLSL cbuffer
                        // bool is 4 -- ConstantBuffer.Write<T> is a raw memcpy
                        // with no knowledge of that mismatch, so plain `bool`
                        // in a cbuffer is a wrong
};

Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);
Texture2D SpecularTexture : register(t1); // r = intensity, g = shininess multiplier, a = reflectivity
SamplerState SpecularSampler : register(s1);
Texture2D NormalTexture : register(t2);
SamplerState NormalSampler : register(s2);

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float3 Normal   : NORMAL;
    [[vk::location(2)]] float3 Tangent  : TANGENT;
    [[vk::location(3)]] float3 Binormal : BINORMAL;
    [[vk::location(4)]] float2 TexCoord : TEXCOORD0;
};

struct SkinnedVSInput
{
    [[vk::location(0)]] float3 Position    : POSITION;
    [[vk::location(1)]] float3 Normal      : NORMAL;
    [[vk::location(2)]] float3 Tangent     : TANGENT;
    [[vk::location(3)]] float3 Binormal    : BINORMAL;
    [[vk::location(4)]] float2 TexCoord    : TEXCOORD0;
    [[vk::location(5)]] float4 BoneIndices : BLENDINDICES0;
    [[vk::location(6)]] float4 BoneWeights : BLENDWEIGHT0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 WorldPosition : TEXCOORD0;
    [[vk::location(1)]] float3 Normal   : NORMAL;
    [[vk::location(2)]] float3 Tangent  : TANGENT;
    [[vk::location(3)]] float3 Binormal : BINORMAL;
    [[vk::location(4)]] float2 TexCoord : TEXCOORD1;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float3 worldPosition = mul(float4(input.Position, 1.0), World).xyz;
    output.Position      = mul(mul(float4(worldPosition, 1.0), View), Projection);
    output.WorldPosition = worldPosition;
    output.Normal   = normalize(mul(float4(input.Normal, 0.0), WorldInverseTranspose).xyz);
    output.Tangent  = normalize(mul(float4(input.Tangent, 0.0), WorldInverseTranspose).xyz);
    output.Binormal = normalize(mul(float4(input.Binormal, 0.0), WorldInverseTranspose).xyz);
    output.TexCoord = input.TexCoord;

    return output;
}

PSInput VSSkinned(SkinnedVSInput input)
{
    float3 position = input.Position;
    float3 normal   = input.Normal;
    ApplySkinning(position, normal, input.BoneIndices, input.BoneWeights);

    VSInput skinned;
    skinned.Position = position;
    skinned.Normal   = normal;
    skinned.Tangent  = input.Tangent;
    skinned.Binormal = input.Binormal;
    skinned.TexCoord = input.TexCoord;

    return VSMain(skinned);
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 diffuseSample  = DiffuseTexture.Sample(DiffuseSampler, input.TexCoord);
    float4 specularSample = SpecularTexture.Sample(SpecularSampler, input.TexCoord);

    if (!Transparent)
    {
        clip(diffuseSample.a - 0.8);
        diffuseSample.a = 1.0;
    }

    float3 bump = (2.0 * NormalTexture.Sample(NormalSampler, input.TexCoord).xyz) - 1.0;
    float3 N = normalize(input.Normal + bump.x * input.Tangent + bump.y * input.Binormal);
    float3 V = normalize(CameraPosition - input.WorldPosition);

    float3 diffuseLight;
    float3 specularLight = CalculateCombinedLighting(
        input.WorldPosition, N, V, specularSample.r, Shininess * specularSample.g, diffuseLight);

    float3 ambient = EvaluateAmbientSH(N);

    float3 reflection = 0;
    if (specularSample.a > 0.01)
        reflection = SampleCubemap(reflect(-V, N)).rgb * specularSample.a;

    float3 albedo = diffuseSample.rgb + reflection;
    float3 lit    = ApplyLight(albedo, diffuseLight + ambient) + specularLight;

    return float4(ApplyFog(float4(lit, diffuseSample.a), input.WorldPosition).rgb, diffuseSample.a);
}

#technique Default vertex=VSMain pixel=PSMain
#technique Skinned vertex=VSSkinned pixel=PSMain
