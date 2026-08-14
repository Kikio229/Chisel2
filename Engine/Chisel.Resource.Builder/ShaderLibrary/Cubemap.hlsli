// ============================================================================
// Cubemap.hlsli
// Reflection probe (cubemap) sampling, with a cheap seam-fix bias since
// hardware seamless-cubemap filtering isn't guaranteed at the GL 3.3 floor
// this project targets.
//
// Include with: #library Cubemap
// ============================================================================
#ifndef CHISEL_CUBEMAP_HLSLI
#define CHISEL_CUBEMAP_HLSLI

// t8/s8 - a fixed high slot so per-shader material textures (t0-t3) never
// collide with it.
TextureCube CubemapTexture : register(t8);
SamplerState CubemapSampler : register(s8);

// b8
cbuffer CubemapConstants : register(b8)
{
    float3 CubemapProbePosition;
    float  CubemapSize;
};

float4 SampleCubemapSeamless(TextureCube tex, SamplerState samp, float3 direction, float texelSize)
{
    float m = max(max(abs(direction.x), abs(direction.y)), abs(direction.z));
    float scale = (texelSize - 1) / texelSize;
    float3 notMax = float3(abs(direction.x) != m, abs(direction.y) != m, abs(direction.z) != m);
    direction = lerp(direction, direction * scale, notMax);
    return tex.Sample(samp, direction);
}

float4 SampleCubemap(float3 direction)
{
    return SampleCubemapSeamless(CubemapTexture, CubemapSampler, direction, CubemapSize);
}

#endif // CHISEL_CUBEMAP_HLSLI
