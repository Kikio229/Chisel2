// ============================================================================
// AmbientProbe.hlsli
// Baked ambient light as 2nd-order spherical harmonics (9 coefficients).
// Models only -- World/Terrain get their ambient term from the lightmap.
//
// Include with: #library AmbientProbe
// ============================================================================
#ifndef CHISEL_AMBIENTPROBE_HLSLI
#define CHISEL_AMBIENTPROBE_HLSLI

#define SH_COEFFICIENT_COUNT 9

// b5
//
// Stored as float4[9] (xyz used, w padding) rather than float3[9]. Array
// uniforms are only memcpy-safe from C# at 16-byte-aligned element types --
// see shader-system.md's array-uniform landmine. A tightly-packed float3[]
// here would silently corrupt every coefficient past the first the moment
// someone calls SetValue with a Vector3[]. The original .fx version of this
// shader used float3[9] directly; that's not safe to carry forward as-is
// under the new ConstantBuffer.Write<T> raw-memcpy model.
cbuffer AmbientProbeConstants : register(b5)
{
    float4 AmbientSH[SH_COEFFICIENT_COUNT];
};

float3 EvaluateAmbientSH(float3 normal)
{
    float3 d = normal;
    return AmbientSH[0].rgb * 0.282095f
         + AmbientSH[1].rgb * (0.488603f * d.y)
         + AmbientSH[2].rgb * (0.488603f * d.z)
         + AmbientSH[3].rgb * (0.488603f * d.x)
         + AmbientSH[4].rgb * (1.092548f * d.x * d.y)
         + AmbientSH[5].rgb * (1.092548f * d.y * d.z)
         + AmbientSH[6].rgb * (0.315392f * (3.0f * d.z * d.z - 1.0f))
         + AmbientSH[7].rgb * (1.092548f * d.x * d.z)
         + AmbientSH[8].rgb * (0.546274f * (d.x * d.x - d.y * d.y));
}

#endif // CHISEL_AMBIENTPROBE_HLSLI
