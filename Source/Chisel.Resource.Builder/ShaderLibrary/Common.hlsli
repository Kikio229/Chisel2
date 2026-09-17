// ============================================================================
// Common.hlsli
// Per-frame constants shared by every shader in the tree. Almost every other
// library includes this one (directly or transitively).
//
// Include with: #library Common
// ============================================================================
#ifndef CHISEL_COMMON_HLSLI
#define CHISEL_COMMON_HLSLI

// b0 - one instance per frame.
cbuffer FrameConstants : register(b0)
{
    float4x4 View;
    float4x4 Projection;
    float3   CameraPosition;
    float    Time;
    float3   CameraForward;
    float2   ScreenSize;
};

#endif // CHISEL_COMMON_HLSLI
