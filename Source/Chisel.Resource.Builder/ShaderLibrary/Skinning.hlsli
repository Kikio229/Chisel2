// ============================================================================
// Skinning.hlsli
// GPU bone skinning (4 influences/vertex). Skinned models only.
//
// Include with: #library Skinning
// ============================================================================
#ifndef CHISEL_SKINNING_HLSLI
#define CHISEL_SKINNING_HLSLI

#define MAX_BONES 200

// b6
cbuffer SkinningConstants : register(b6)
{
    float4x3 Bones[MAX_BONES];
};

// Row-major, row-vector convention throughout this project: mul(vertex, bone).
// Hardcoded to 4 bones/vertex (matching every skinned vertex format currently
// in use); if a format with a different influence count ever shows up, add a
// boneCount parameter back the way the original .fx file had it.
void ApplySkinning(inout float3 position, inout float3 normal, float4 boneIndices, float4 boneWeights)
{
    float4x3 skin = 0;

    [unroll]
    for (int i = 0; i < 4; i++)
        skin += Bones[(int)boneIndices[i]] * boneWeights[i];

    position = mul(float4(position, 1.0), skin);
    normal   = mul(normal, (float3x3)skin);
}

#endif // CHISEL_SKINNING_HLSLI
