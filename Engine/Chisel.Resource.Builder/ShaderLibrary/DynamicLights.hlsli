// ============================================================================
// DynamicLights.hlsli
// Per-frame realtime point/spot lights (players, projectiles, muzzle flashes,
// anything that moves). Shared by every lit shader: models, world brushes,
// terrain.
//
// Include with: #library DynamicLights
// ============================================================================
#ifndef CHISEL_DYNAMICLIGHTS_HLSLI
#define CHISEL_DYNAMICLIGHTS_HLSLI

#define MAX_REALTIME_LIGHTS 32

// b2
cbuffer RealtimeLightConstants : register(b2)
{
    int    RealtimeLightCount;
    float4 RealtimeLightPositions[MAX_REALTIME_LIGHTS]; // xyz = world pos, w = range
    float4 RealtimeLightColors[MAX_REALTIME_LIGHTS];    // rgb = color, a = intensity
    float4 RealtimeLightSpotData[MAX_REALTIME_LIGHTS];  // xyz = spot direction, w = half-angle in radians, 0 = omni
};

float3 ApplyLight(float3 baseColor, float3 lightColor)
{
    return baseColor * lightColor;
}

// Adds every in-range realtime light's contribution to baseColor. This is the
// cheap path -- diffuse (N-dot-L) only, no specular. World, Terrain, and
// SimpleModel use this directly. Model.hlsl uses the fuller combine in
// StaticLights.hlsli instead, which also folds these same lights in.
float3 ApplyRealtimeLights(float3 baseColor, float3 normal, float3 worldPosition)
{
    float3 result = baseColor;

    [loop]
    for (int i = 0; i < RealtimeLightCount; i++)
    {
        float3 toLight  = RealtimeLightPositions[i].xyz - worldPosition;
        float3 lightDir = normalize(toLight);
        float  atten    = saturate((RealtimeLightPositions[i].w - length(toLight)) / max(RealtimeLightPositions[i].w, 0.0001));
        atten *= dot(lightDir, normal) * 0.5 + 0.5;

        float spotAngle = RealtimeLightSpotData[i].w;
        if (spotAngle > 0)
        {
            float angle = acos(dot(-lightDir, RealtimeLightSpotData[i].xyz));
            atten *= sqrt(saturate(spotAngle - angle) / spotAngle);
        }

        result += RealtimeLightColors[i].rgb * RealtimeLightColors[i].a * atten;
    }

    return result;
}

#endif // CHISEL_DYNAMICLIGHTS_HLSLI
