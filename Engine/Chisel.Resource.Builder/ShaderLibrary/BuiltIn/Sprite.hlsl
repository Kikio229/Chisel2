#pragma pack_matrix(row_major)

#technique Default vertex=VSMain pixel=PSMain
#technique Color vertex=VSMain pixel=PSColor

cbuffer ObjectData : register(b0)
{
    float4x4 ViewProjection;
};

Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
    [[vk::location(1)]] float2 UV : TEXCOORD0;
    [[vk::location(2)]] float4 Color : COLOR;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float2 UV : TEXCOORD0;
    [[vk::location(1)]] float4 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    output.UV = input.UV;
    output.Color = input.Color;
    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 texColor = DiffuseTexture.Sample(DiffuseSampler, input.UV);
    return texColor * input.Color;
}

float4 PSColor(PSInput input) : SV_TARGET
{
    return input.Color;
}