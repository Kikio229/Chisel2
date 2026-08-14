// ============================================================================
// Lightmap.hlsli
// 3-basis ("radiosity normal mapped" / Valve-style) baked lightmap sampling,
// bicubic-filtered. Used by World and Terrain.
//
// The three basis textures are a fixed part of the binding contract -- always
// declared here, always at t10-t12/s10-s12, so World and Terrain (and any
// future lightmapped shader) share one correct definition instead of each
// re-declaring their own copy.
//
// Include with: #library Lightmap
// ============================================================================
#ifndef CHISEL_LIGHTMAP_HLSLI
#define CHISEL_LIGHTMAP_HLSLI

Texture2D LightmapBasis1Texture : register(t10);
SamplerState LightmapBasis1Sampler : register(s10);
Texture2D LightmapBasis2Texture : register(t11);
SamplerState LightmapBasis2Sampler : register(s11);
Texture2D LightmapBasis3Texture : register(t12);
SamplerState LightmapBasis3Sampler : register(s12);

// b9
cbuffer LightmapConstants : register(b9)
{
    float2 LightmapSize;
    int    DisableLighting; // int, not bool -- see the note in Model.hlsl's
    int    ShowLightmap;    // MaterialConstants about cbuffer bool marshaling.
};

// Fixed lightmap basis directions (identical to the offline lightmap baker's
// B1/B2/B3), reconstructed per-vertex from the surface TBN rather than read
// from a per-draw-call uniform -- needed since one draw call can span many
// differently oriented faces once batched together by material.
static const float3 LIGHTMAP_BASIS_1 = float3(0.8164966, 0.0, 0.5773503);
static const float3 LIGHTMAP_BASIS_2 = float3(-0.4082483, 0.7071068, 0.5773503);
static const float3 LIGHTMAP_BASIS_3 = float3(-0.4082483, -0.7071068, 0.5773503);

void ComputeLightmapBasis(float3 tangent, float3 binormal, float3 normal,
    out float3 basis1, out float3 basis2, out float3 basis3)
{
    basis1 = LIGHTMAP_BASIS_1.x * tangent + LIGHTMAP_BASIS_1.y * binormal + LIGHTMAP_BASIS_1.z * normal;
    basis2 = LIGHTMAP_BASIS_2.x * tangent + LIGHTMAP_BASIS_2.y * binormal + LIGHTMAP_BASIS_2.z * normal;
    basis3 = LIGHTMAP_BASIS_3.x * tangent + LIGHTMAP_BASIS_3.y * binormal + LIGHTMAP_BASIS_3.z * normal;
}

float4 CubicWeights(float v)
{
    float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
    float4 s = n * n * n;
    float x = s.x;
    float y = s.y - 4.0 * s.x;
    float z = s.z - 4.0 * s.y + 6.0 * s.x;
    float w = 6.0 - x - y - z;
    return float4(x, y, z, w) * (1.0 / 6.0);
}

// Bicubic-filtered lightmap sample -- smooths out the lightmap's much lower
// texel density relative to the surface it's projected onto.
float4 SampleLightmapBicubic(Texture2D tex, SamplerState samp, float2 uv)
{
    float2 texSize = LightmapSize;
    float2 invTexSize = 1.0 / texSize;

    float2 texCoords = uv * texSize - 0.5;
    float2 fxy = frac(texCoords);
    texCoords -= fxy;

    float4 xcubic = CubicWeights(fxy.x);
    float4 ycubic = CubicWeights(fxy.y);

    float4 c = texCoords.xxyy + float2(-0.5, 1.5).xyxy;
    float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
    float4 offset = (c + float4(xcubic.yw, ycubic.yw) / s) * invTexSize.xxyy;

    float4 sample0 = tex.Sample(samp, offset.xz);
    float4 sample1 = tex.Sample(samp, offset.yz);
    float4 sample2 = tex.Sample(samp, offset.xw);
    float4 sample3 = tex.Sample(samp, offset.yw);

    float sx = s.x / (s.x + s.y);
    float sy = s.z / (s.z + s.w);

    return lerp(lerp(sample3, sample2, sx), lerp(sample1, sample0, sx), sy);
}

// Samples and combines all three basis lightmaps, weighted by how much the
// surface normal aligns with each basis direction.
float3 SampleLightmap(float2 uv, float3 basis1, float3 basis2, float3 basis3, float3 normal)
{
    float3 lm1 = SampleLightmapBicubic(LightmapBasis1Texture, LightmapBasis1Sampler, uv).rgb;
    float3 lm2 = SampleLightmapBicubic(LightmapBasis2Texture, LightmapBasis2Sampler, uv).rgb;
    float3 lm3 = SampleLightmapBicubic(LightmapBasis3Texture, LightmapBasis3Sampler, uv).rgb;

    float3 lit = lm1 * saturate(dot(normal, basis1))
               + lm2 * saturate(dot(normal, basis2))
               + lm3 * saturate(dot(normal, basis3));

    return (DisableLighting != 0 && ShowLightmap == 0) ? float3(1, 1, 1) : lit;
}

#endif // CHISEL_LIGHTMAP_HLSLI
