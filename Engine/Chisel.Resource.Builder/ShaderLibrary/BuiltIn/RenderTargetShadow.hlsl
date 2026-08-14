// ============================================================================
// RenderTargetShadow.hlsl
// A soft-shadowed "blob" decal: samples a shadow map with a 4-tap PCF blur,
// modulates it by a hand-authored falloff blob texture, and blends toward
// ShadowColor. Used to fake soft contact shadows under dynamic objects
// without a real shadow-mapping pass.
//
// Load with: Content.Load<ShaderProgram>("Shaders/RenderTargetShadow")
// ============================================================================
#pragma pack_matrix(row_major)

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
    float4x4 View;
    float4x4 Projection;
};

// b11
cbuffer DecalConstants : register(b11)
{
    float2 ShadowMapTexelSize; // 1 / shadow map resolution, precomputed on the CPU
    float3 ShadowColor;
};

Texture2D ShadowMapTexture : register(t0); // bind with a bilinear ISampler
SamplerState ShadowMapSampler : register(s0);
Texture2D BlobTexture : register(t1);      // bind with a point/nearest ISampler
SamplerState BlobSampler : register(s1);

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float2 TexCoord : TEXCOORD0;
    [[vk::location(2)]] float4 Color    : COLOR0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float2 TexCoord : TEXCOORD0;
    [[vk::location(1)]] float4 Color    : COLOR1;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float4 worldPosition = mul(float4(input.Position, 1.0), World);
    float4 viewPosition  = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.TexCoord = input.TexCoord;
    output.Color    = input.Color;

    return output;
}

float CalcShadowTermSoftPCF(float2 uv)
{
    float2 texel = ShadowMapTexelSize;
    float shadow = 0;
    shadow += ShadowMapTexture.Sample(ShadowMapSampler, uv + float2(-texel.x, -texel.y)).r;
    shadow += ShadowMapTexture.Sample(ShadowMapSampler, uv + float2( texel.x, -texel.y)).r;
    shadow += ShadowMapTexture.Sample(ShadowMapSampler, uv + float2(-texel.x,  texel.y)).r;
    shadow += ShadowMapTexture.Sample(ShadowMapSampler, uv + float2( texel.x,  texel.y)).r;
    return shadow / 4.0;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float  shadowTerm  = CalcShadowTermSoftPCF(input.TexCoord * float2(1, -1));
    float4 blobColor   = BlobTexture.Sample(BlobSampler, input.TexCoord);
    float  shadowDepth = 1 - input.Color.w;
    float  shadowIntensity = (1 - shadowTerm) * (blobColor.a * 0.5 + 0.5) * 0.5 * shadowDepth;

    float3 color = lerp(ShadowColor, float3(1, 1, 1), 1 - shadowIntensity);
    return float4(color, 1);
}

#technique Default vertex=VSMain pixel=PSMain
