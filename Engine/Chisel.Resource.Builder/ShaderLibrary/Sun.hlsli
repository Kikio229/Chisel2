// ============================================================================
// Sun.hlsli
// One directional "sun" light for the whole scene. Currently only consumed by
// StaticLights.hlsli (i.e. only models react to it) -- the original World/
// Terrain shaders declared their own copies of these same uniforms but never
// actually referenced them in a pixel shader, so that dead wiring wasn't
// carried forward. See the port README for details.
//
// Include with: #library Sun
// ============================================================================
#ifndef CHISEL_SUN_HLSLI
#define CHISEL_SUN_HLSLI

// b3
cbuffer SunConstants : register(b3)
{
    float3 SunDirection;
    float  SunIntensity;
    float4 SunColor;
};

#endif // CHISEL_SUN_HLSLI
