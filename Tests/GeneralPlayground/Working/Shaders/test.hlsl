#pragma pack_matrix(row_major)

cbuffer Transform : register(b0)
{
    float4x4 WorldViewProjection;
};

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float3 Color : COLOR;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 1.0), WorldViewProjection);
    output.Color = input.Color;
    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return float4(input.Color, 1.0);
}