// ============================================================================
// Fog.hlsli
// Simple linear distance fog, applied in world space against the camera.
//
// Include with: #library Fog
// ============================================================================
#ifndef CHISEL_FOG_HLSLI
#define CHISEL_FOG_HLSLI

#library Common

// b1
cbuffer FogConstants : register(b1)
{
    float4 FogColor;
    float  FogIntensity;
    float  FogStart;
    float  FogEnd;
};

float4 ApplyFog(float4 baseColor, float3 worldPosition)
{
    // max(..., 0.0001) guards the original's unguarded (FogEnd - FogStart)
    // divide-by-zero if the two are ever set equal.
    float dist = saturate((distance(worldPosition, CameraPosition) - FogStart) / max(FogEnd - FogStart, 0.0001));
    return float4(lerp(baseColor.rgb, FogColor.rgb, dist * FogIntensity), baseColor.a);
}

#endif // CHISEL_FOG_HLSLI
