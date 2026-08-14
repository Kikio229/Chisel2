// ============================================================================
// World.hlsl
// BSP brush geometry: diffuse + specular + normal map, 3-basis baked
// lightmap, cubemap reflections, realtime lights, fog. Also supports the
// "3D skybox" mini-render trick (see SkyView.hlsli) and a wireframe-expand
// debug mode.
//
// Load with: Content.Load<ShaderProgram>("Shaders/World")
// ============================================================================
#pragma pack_matrix(row_major)

#library Common
#library Fog
#library DynamicLights
#library Cubemap
#library Lightmap
#library SkyView

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
};

// b11
cbuffer MaterialConstants : register(b11)
{
    int ExpandWireframe; // see Model.hlsl's MaterialConstants for why this is `int`
};

Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);
Texture2D SpecularTexture : register(t1);
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
    [[vk::location(5)]] float2 LightmapCoord : TEXCOORD1;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 WorldPosition : TEXCOORD0;
    [[vk::location(1)]] float3 Normal   : NORMAL;
    [[vk::location(2)]] float3 Tangent  : TANGENT;
    [[vk::location(3)]] float3 Binormal : BINORMAL;
    [[vk::location(4)]] float2 TexCoord : TEXCOORD1;
    [[vk::location(5)]] float2 LightmapCoord : TEXCOORD2;
    [[vk::location(6)]] float3 LightmapBasis1 : TEXCOORD3;
    [[vk::location(7)]] float3 LightmapBasis2 : TEXCOORD4;
    [[vk::location(8)]] float3 LightmapBasis3 : TEXCOORD5;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float3 localPosition = input.Position + (ExpandWireframe != 0 ? input.Normal * 0.0001 : float3(0, 0, 0));
    float3 worldPosition = mul(float4(localPosition, 1.0), World).xyz;
    float4 clipPosition  = mul(mul(float4(worldPosition, 1.0), View), Projection);

    output.Position = clipPosition;
    output.WorldPosition = ApplySkyboxViewRemap(float4(worldPosition, clipPosition.w), View, Projection).xyz;

    output.Normal   = input.Normal;
    output.Tangent  = input.Tangent;
    output.Binormal = input.Binormal;
    output.TexCoord = input.TexCoord;
    output.LightmapCoord = input.LightmapCoord;

    ComputeLightmapBasis(input.Tangent, input.Binormal, input.Normal,
        output.LightmapBasis1, output.LightmapBasis2, output.LightmapBasis3);

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 diffuseSample = DiffuseTexture.Sample(DiffuseSampler, input.TexCoord);
    clip(diffuseSample.a - 0.01);

    float3 bump = (2.0 * NormalTexture.Sample(NormalSampler, input.TexCoord).xyz) - 1.0;
    float3 bumpNormal = input.Normal + bump.x * input.Tangent - bump.y * input.Binormal;

    float4 specularSample = SpecularTexture.Sample(SpecularSampler, input.TexCoord);
    float3 reflection = 0;
    if (specularSample.a > 0.01)
    {
        float3 viewDir = normalize(input.WorldPosition - CubemapProbePosition);
        reflection = SampleCubemap(reflect(viewDir, normalize(bumpNormal))).rgb * specularSample.a;
    }

    float3 lightmapColor = SampleLightmap(input.LightmapCoord,
        input.LightmapBasis1, input.LightmapBasis2, input.LightmapBasis3, bumpNormal);

    float3 litColor = ApplyRealtimeLights(lightmapColor, bumpNormal, input.WorldPosition);

    float4 albedo = diffuseSample + float4(reflection, 0);
    return ApplyFog(float4(ApplyLight(albedo.rgb, litColor), albedo.a), input.WorldPosition);
}

#technique Default vertex=VSMain pixel=PSMain
