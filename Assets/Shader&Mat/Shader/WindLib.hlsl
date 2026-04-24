#ifndef CUSTOM_WIND_INCLUDED
#define CUSTOM_WIND_INCLUDED

// 严格匹配 Shader 调用的展开参数版本
float2 CalculateWindOffset(int layerIndex, int totalLayers, float3 worldPos, float strength, float frequency, float4 direction, float turbulence, float time)
{
    float t = time * frequency;
    float heightRatio = (float)layerIndex / (float)max(1, totalLayers - 1);
    
    // 保持你原始的“像真实树干”的位移曲线
    float influence = (heightRatio * heightRatio) + 1;
    float phase = worldPos.x * 0.37 + worldPos.z * 0.29;

    float sway = sin(t + phase) * strength;
    float turb = sin(t * 2.7 + phase * 1.5) * strength * turbulence;

    float2 windDir = normalize(direction.xy);
    return windDir * (sway + turb) * influence;
}
#endif