// ============================================================================
// Skybox.hlsl
//
// Replaces Skybox.fx.
//
// Load with: Content.Load<ShaderProgram>("Shaders/Skybox")
// ============================================================================
#pragma pack_matrix(row_major)

#library Common
#library Cubemap

// b10
cbuffer TransformConstants : register(b10)
{
    float4x4 World;
};

struct VSInput
{
    [[vk::location(0)]] float3 Position : POSITION;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    [[vk::location(0)]] float3 Direction : TEXCOORD0;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float3 worldPosition = mul(float4(input.Position, 1.0), World).xyz;
    output.Position  = mul(mul(float4(worldPosition, 1.0), View), Projection);
    output.Direction = worldPosition;

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return SampleCubemap(normalize(input.Direction));
}

#technique Default vertex=VSMain pixel=PSMain
