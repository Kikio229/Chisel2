// ============================================================================
// Terrain.hlsl
// Blended terrain (two tiling textures blended by a per-vertex factor plus a
// blend-mask texture), normal-mapped, 3-basis baked lightmap, realtime
// lights, fog, "3D skybox" support.
//
// Replaces TerrainDefault.fx.
//
// Load with: Content.Load<ShaderProgram>("Shaders/Terrain")
// ============================================================================
#pragma pack_matrix(row_major)

#library Common
#library Fog
#library DynamicLights
#library Lightmap
#library SkyView

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
};

Texture2D DiffuseTexture : register(t0);   // primary tiling texture
SamplerState DiffuseSampler : register(s0);
Texture2D NormalTexture : register(t1);
SamplerState NormalSampler : register(s1);
Texture2D BlendTexture : register(t2);     // secondary tiling texture, blended in
SamplerState BlendSampler : register(s2);
Texture2D BlendMaskTexture : register(t3); // per-texel blend bias between Diffuse/Blend
SamplerState BlendMaskSampler : register(s3);

struct VSInput
{
    [[vk::location(0)]] float3 Position    : POSITION;
    [[vk::location(1)]] float3 Normal      : NORMAL;
    [[vk::location(2)]] float4 Tangent     : TANGENT;    // w = binormal handedness
    [[vk::location(3)]] float2 TexCoord    : TEXCOORD0;
    [[vk::location(4)]] float  BlendFactor : TEXCOORD1;
    [[vk::location(5)]] float2 LightmapCoord : TEXCOORD2;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 WorldPosition : TEXCOORD0;
    [[vk::location(1)]] float3 Normal   : NORMAL;
    [[vk::location(2)]] float3 Tangent  : TANGENT;
    [[vk::location(3)]] float3 Binormal : BINORMAL;
    [[vk::location(4)]] float2 TexCoord : TEXCOORD1;
    [[vk::location(5)]] float  BlendFactor : TEXCOORD2;
    [[vk::location(6)]] float2 LightmapCoord : TEXCOORD3;
    [[vk::location(7)]] float3 LightmapBasis1 : TEXCOORD4;
    [[vk::location(8)]] float3 LightmapBasis2 : TEXCOORD5;
    [[vk::location(9)]] float3 LightmapBasis3 : TEXCOORD6;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float3 worldPosition = mul(float4(input.Position, 1.0), World).xyz;
    float4 clipPosition  = mul(mul(float4(worldPosition, 1.0), View), Projection);
    output.Position = clipPosition;
    output.WorldPosition = ApplySkyboxViewRemap(float4(worldPosition, clipPosition.w), View, Projection).xyz;

    // Terrain's vertex format carries tangent + handedness rather than an
    // explicit binormal (unlike World/Model) -- reconstruct it here.
    float3 N = normalize(mul(float4(input.Normal, 0.0), World).xyz);
    float3 T = normalize(mul(float4(input.Tangent.xyz, 0.0), World).xyz);
    float3 B = cross(N, T) * input.Tangent.w;

    output.Normal   = N;
    output.Tangent  = T;
    output.Binormal = B;
    output.TexCoord = input.TexCoord;
    output.BlendFactor = input.BlendFactor;
    output.LightmapCoord = input.LightmapCoord;

    ComputeLightmapBasis(T, B, N, output.LightmapBasis1, output.LightmapBasis2, output.LightmapBasis3);

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float blendMask = BlendMaskTexture.Sample(BlendMaskSampler, input.TexCoord).r;
    float4 diffuseSample = lerp(
        BlendTexture.Sample(BlendSampler, input.TexCoord),
        DiffuseTexture.Sample(DiffuseSampler, input.TexCoord),
        saturate(input.BlendFactor + blendMask));

    clip(diffuseSample.a - 0.01);

    float3 bump = (2.0 * NormalTexture.Sample(NormalSampler, input.TexCoord).xyz) - 1.0;
    float3 bumpNormal = normalize(input.Normal + bump.x * input.Tangent - bump.y * input.Binormal);

    float3 lightmapColor = SampleLightmap(input.LightmapCoord,
        input.LightmapBasis1, input.LightmapBasis2, input.LightmapBasis3, bumpNormal);

    float3 litColor = ApplyRealtimeLights(lightmapColor, bumpNormal, input.WorldPosition);

    return ApplyFog(float4(ApplyLight(diffuseSample.rgb, litColor), diffuseSample.a), input.WorldPosition);
}

#technique Default vertex=VSMain pixel=PSMain
