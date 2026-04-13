using QFramework.System;
using UnityEngine;


[System.Serializable]
public class PlanetNodeMapData
{
    public Vector3 SurfaceNormal;
    public Vector3 Uv3D;
    public float HeightNoise;
    public float MoistureNoise;
    public float LandHeight01;
    public bool IsLand;
    public bool IsSunlit;
    public PlanetTerrainType TerrainTierType;
    public PlanetMoistureType MoistureBandType;

    public static PlanetNodeMapData Create(
        PlanetGenerator.PlanetSettings settings,
        Vector3 surfaceNormal,
        Vector3 uv3d,
        float heightNoise,
        float moistureNoise,
        Vector3 mainLightDirection,
        float sunlitDotThreshold)
    {
        PlanetNodeMapData data = new PlanetNodeMapData
        {
            SurfaceNormal = surfaceNormal.normalized,
            Uv3D = uv3d,
            HeightNoise = heightNoise,
            MoistureNoise = moistureNoise,
        };

        data.IsLand = heightNoise >= settings.seaLevel;
        data.LandHeight01 = data.IsLand ? Mathf.InverseLerp(settings.seaLevel, 1.0f, heightNoise) : 0.0f;

        Vector3 lightDir = Vector3.up;
        if(mainLightDirection.sqrMagnitude > 0.00001f) // 如果光源方向不为0，则使用光源方向
        {
            lightDir = mainLightDirection.normalized;
        }

        // 计算光照方向与表面法线的点积，如果大于阈值，则认为光照方向与表面法线方向一致，否则认为光照方向与表面法线方向不一致  
        data.IsSunlit = Vector3.Dot(data.SurfaceNormal, lightDir) >= sunlitDotThreshold; 

        // 计算湿度
        data.MoistureBandType = PlanetMoistureType.Dry;
        if(moistureNoise >= settings.moistureThreshold)
        {
            data.MoistureBandType = PlanetMoistureType.Wet;
        }


        data.TerrainTierType = ResolveTerrainTier(settings, data.IsLand, data.LandHeight01, uv3d, heightNoise);
        return data;
    }

    private static PlanetTerrainType ResolveTerrainTier(
        PlanetGenerator.PlanetSettings settings,
        bool isLand,
        float landHeight01,
        Vector3 uv3d,
        float heightNoise)
    {   
        if (!isLand) 
        {
            return PlanetTerrainType.Ocean;
        }

        // 计算本地坐标
        Vector3 localPos = (uv3d - (Vector3.one * 0.5f)) * 2.0f;

        // 计算距离地表的距离
        float distY = Mathf.Abs(localPos.y) + (heightNoise - 0.5f) * 0.5f;

        // 计算是否为极地
        bool isPolar = distY >= settings.poleThreshold;
        if (isPolar) 
        {
            return PlanetTerrainType.Polar;
        }

        // 修复：使用原始 heightNoise 而不是 landHeight01，与 shader 保持一致
        // 这样阈值的含义就统一了：都是针对完整高度范围 [0, 1] 的
        // 注意：shoreThreshold 是海岸线，应该在 seaLevel 之上才是陆地
        if (heightNoise < settings.plain1Threshold) return PlanetTerrainType.Plain1;
        if (heightNoise < settings.plain2Threshold) return PlanetTerrainType.Plain2;
        if (heightNoise < settings.mountain1Threshold) return PlanetTerrainType.Mountain1;
        if (heightNoise < settings.mountain2Threshold) return PlanetTerrainType.Mountain2;
        return PlanetTerrainType.Snow;
    }
}
