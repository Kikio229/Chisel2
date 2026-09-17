// ============================================================================
// SkyView.hlsli
//
// The main purpose of this util is to allow sky-world geom to get lit by
// realtime lights.
//
// Include with: #library SkyView
// ============================================================================
#ifndef CHISEL_SKYVIEW_HLSLI
#define CHISEL_SKYVIEW_HLSLI

// b7
cbuffer SkyViewConstants : register(b7)
{
    float3 SkyboxViewTranslation; // MaterialConstants about cbuffer bool marshaling.
    int    Skybox3DView; // int, not bool -- see the note in Model.hlsl's
};

// Call right after computing a vertex's world position and clip-space W.
// Returns worldPosition unchanged unless Skybox3DView is set.
float4 ApplySkyboxViewRemap(float4 worldPosition, float4x4 view, float4x4 projection)
{
    if (Skybox3DView == 0)
        return worldPosition;

    float4 remapped = (worldPosition - float4(SkyboxViewTranslation, 0)) * 16;
    float4 fakeViewPosition = mul(remapped, view);
    remapped.w = mul(fakeViewPosition, projection).w;
    return remapped;
}

#endif // CHISEL_SKYVIEW_HLSLI
