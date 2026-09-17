// ============================================================================
// SimpleModel.hlsl
// Cheap vertex-colored, realtime-lit-only model shader for props/debris where
// full normal-mapped/specular/SH-ambient shading (Model.hlsl) isn't worth the
// cost. No static lights, no ambient probe, no cubemap reflections -- just a
// diffuse texture tinted by vertex color and realtime lights.
//
// Replaces PropModel.fx. The old file's two passes (main draw + a
// depth-prepass that only writes fully-opaque texels) are now two techniques.
//
// Load with: Content.Load<ShaderEffect>("Shaders/SimpleModel")
//   shader.SetTechnique("Default");   // or "DepthPrepass"
// ============================================================================
#pragma pack_matrix(row_major)

#library Common
#library Fog
#library DynamicLights

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
};

// b11
cbuffer MaterialConstants : register(b11)
{
    int Transparent; // see Model.hlsl's MaterialConstants for why this is `int`
};

Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float3 Normal   : NORMAL;
    [[vk::location(2)]] float4 Color    : COLOR0;
    [[vk::location(3)]] float2 TexCoord : TEXCOORD0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 WorldPosition : TEXCOORD0;
    [[vk::location(1)]] float3 Normal : NORMAL;
    [[vk::location(2)]] float4 Color  : COLOR0;
    [[vk::location(3)]] float2 TexCoord : TEXCOORD1;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float3 worldPosition = mul(float4(input.Position, 1.0), World).xyz;
    output.Position      = mul(mul(float4(worldPosition, 1.0), View), Projection);
    output.WorldPosition = worldPosition;
    output.Normal   = input.Normal;
    output.TexCoord = input.TexCoord;
    output.Color    = ApplyFog(input.Color, worldPosition);

    return output;
}

// Depth-prepass vertex shader is identical except it doesn't fog the vertex
// color -- matches the original PrePassVS, which fed an unmodified color into
// PrePassPS purely to clip on alpha.
PSInput VSDepthPrepass(VSInput input)
{
    PSInput output;

    float3 worldPosition = mul(float4(input.Position, 1.0), World).xyz;
    output.Position      = mul(mul(float4(worldPosition, 1.0), View), Projection);
    output.WorldPosition = worldPosition;
    output.Normal   = input.Normal;
    output.TexCoord = input.TexCoord;
    output.Color    = input.Color;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 diffuseSample = DiffuseTexture.Sample(DiffuseSampler, input.TexCoord);

    if (!Transparent)
    {
        clip(diffuseSample.a - 0.5);
        diffuseSample.a = 1.0;
    }
    if (diffuseSample.a > 0.6)
        diffuseSample.a = 1.0;

    float3 lit = ApplyRealtimeLights(input.Color.rgb, input.Normal, input.WorldPosition);
    return float4(ApplyLight(diffuseSample.rgb, lit), diffuseSample.a);
}

float4 PSDepthPrepass(PSInput input) : SV_TARGET
{
    float4 diffuseSample = DiffuseTexture.Sample(DiffuseSampler, input.TexCoord);
    clip(diffuseSample.a - 0.6);
    return float4(diffuseSample.rgb, 1.0);
}

#technique Default vertex=VSMain pixel=PSMain
#technique DepthPrepass vertex=VSDepthPrepass pixel=PSDepthPrepass
