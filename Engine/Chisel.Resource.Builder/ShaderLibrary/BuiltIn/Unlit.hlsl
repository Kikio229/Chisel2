// ============================================================================
// Unlit.hlsl
//
// Load with: Content.Load<ShaderProgram>("Shaders/Unlit")
// ============================================================================
#pragma pack_matrix(row_major)

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 WorldViewProjection;
};

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float4 Color    : COLOR0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float4 Color : COLOR0;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 1.0), WorldViewProjection);
    output.Color    = input.Color;
    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}

#technique Default vertex=VSMain pixel=PSMain
