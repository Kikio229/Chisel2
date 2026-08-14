// ============================================================================
// StaticLights.hlsli
// Lights baked per-object at content-build time (the "closest N static lights"
// list), for movable geometry that can't sample a lightmap -- i.e. models.
// World and Terrain use real lightmaps instead (see Lightmap.hlsli) and never
// need this cbuffer.
//
// This is also where the full diffuse+specular light combine lives, since it's
// the thing that actually needs the sun + static lights + realtime lights all
// at once. Shaders that only need cheap diffuse realtime lighting (World,
// Terrain, SimpleModel) should use DynamicLights.hlsli's ApplyRealtimeLights
// directly instead of pulling this whole library in.
//
// Include with: #library StaticLights   (pulls in DynamicLights + Sun too)
// ============================================================================
#ifndef CHISEL_STATICLIGHTS_HLSLI
#define CHISEL_STATICLIGHTS_HLSLI

#library DynamicLights
#library Sun

#define MAX_STATIC_LIGHTS 4

// b4
cbuffer StaticLightConstants : register(b4)
{
    int    StaticLightCount;
    float4 StaticLightPositions[MAX_STATIC_LIGHTS]; // xyz = pos, w = range
    float4 StaticLightColors[MAX_STATIC_LIGHTS];    // rgb = color, a = intensity
    float4 StaticLightAngles[MAX_STATIC_LIGHTS];    // xyz = heading, w = spot half-angle in radians, 0 = omni
};

float SpecularTerm(float3 lightDir, float3 viewDir, float3 normal, float shininess)
{
    float3 r     = normalize(2.0 * dot(normal, lightDir) * normal - lightDir);
    float ndotl  = max(0.0001, dot(normal, lightDir));
    float rdotv  = max(0.0, dot(r, viewDir));
    return ndotl * pow(rdotv, shininess);
}

// One light's diffuse+specular contribution. Shared by the sun, the static
// light loop, and the realtime light loop below -- in the original code this
// same four-line shape was hand-copied three times.
void AccumulateLight(float3 lightDir, float3 lightColor, float3 N, float3 V,
    float specularIntensity, float shininess, inout float3 diffuseOut, inout float3 specularOut)
{
    diffuseOut  += lightColor * (dot(lightDir, N) * 0.5 + 0.5);
    specularOut += saturate(specularIntensity * max(SpecularTerm(lightDir, V, N, shininess), 0) * lightColor);
}

// Combines the scene's sun, this object's baked static point lights, and all
// in-range realtime lights into one diffuse+specular result.
float3 CalculateCombinedLighting(float3 worldPosition, float3 N, float3 V,
    float specularIntensity, float shininess, out float3 diffuseOut)
{
    float3 diffuse  = float3(0, 0, 0);
    float3 specular = float3(0, 0, 0);

    AccumulateLight(SunDirection, SunColor.rgb * SunIntensity, N, V, specularIntensity, shininess, diffuse, specular);

    [loop]
    for (int i = 0; i < StaticLightCount; i++)
    {
        float3 toLight  = StaticLightPositions[i].xyz - worldPosition;
        float3 lightDir = normalize(toLight);
        float  falloff  = 1 - saturate(length(toLight) / StaticLightPositions[i].w);
        float3 lightColor = StaticLightColors[i].rgb * StaticLightColors[i].a * (falloff * falloff);

        float theta = acos(dot(lightDir, normalize(StaticLightAngles[i].xyz)));
        lightColor *= (theta > StaticLightAngles[i].w && StaticLightAngles[i].w > 0) ? 0.0 : 1.0;

        AccumulateLight(lightDir, lightColor, N, V, specularIntensity, shininess, diffuse, specular);
    }

    [loop]
    for (int j = 0; j < RealtimeLightCount; j++)
    {
        float3 toLight  = RealtimeLightPositions[j].xyz - worldPosition;
        float3 lightDir = normalize(toLight);
        float  atten    = saturate((RealtimeLightPositions[j].w - length(toLight)) / max(RealtimeLightPositions[j].w, 0.0001));
        float3 lightColor = RealtimeLightColors[j].rgb * RealtimeLightColors[j].a * atten;

        float spotAngle = RealtimeLightSpotData[j].w;
        if (spotAngle > 0)
        {
            float angle = acos(dot(-lightDir, RealtimeLightSpotData[j].xyz));
            lightColor *= sqrt(saturate(spotAngle - angle) / spotAngle);
        }

        AccumulateLight(lightDir, lightColor, N, V, specularIntensity, shininess, diffuse, specular);
    }

    diffuseOut = diffuse;
    return specular;
}

#endif // CHISEL_STATICLIGHTS_HLSLI
