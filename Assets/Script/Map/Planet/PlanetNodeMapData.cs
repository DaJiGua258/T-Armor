using QFramework.System;
using UnityEngine;
using QFramework.Model;


[System.Serializable]
public class PlanetNodeMapData
{
    // public Vector3 SurfaceNormal;
    public float HeightNoise;
    public float MoistureNoise;
    public float LandHeight01;
    public int Seed;
    public bool IsLand;
    public bool IsSunlit;
    public EnvironmentData environmentData = new EnvironmentData();


    public PlanetNodeMapData Create(
        PlanetGenerator.PlanetSettings settings,
        Vector3 surfaceNormal,
        Vector3 uv3d,
        float heightNoise,
        float moistureNoise,
        Vector3 mainLightDirection,
        float sunlitDotThreshold)
    {
        // ----- 初始化 -------------------------
        PlanetNodeMapData data = new PlanetNodeMapData();
        data.environmentData.SurfaceNormal = surfaceNormal.normalized;
        // data.SurfaceNormal = surfaceNormal.normalized;
        data.HeightNoise = heightNoise;
        data.MoistureNoise = moistureNoise;

        // ----- 开始计算地图信息 -------------------------
        data.IsLand = heightNoise >= settings.seaLevel;

        Vector3 lightDir = Vector3.up;
        if(mainLightDirection.sqrMagnitude > 0.00001f) // 如果光源方向不为0，则使用光源方向
        {
            lightDir = mainLightDirection.normalized;
        }

        // 计算光照方向与表面法线的点积，如果大于阈值，则认为光照方向与表面法线方向一致，否则认为光照方向与表面法线方向不一致  
        data.IsSunlit = Vector3.Dot(data.environmentData.SurfaceNormal, lightDir) >= sunlitDotThreshold; 

        // 计算湿度
        data.environmentData.moistureType = MoistureType.Dry;
        if(moistureNoise >= settings.moistureThreshold)
        {
            data.environmentData.moistureType = MoistureType.Wet;
        }


        data.environmentData.terrainType = ResolveTerrainTier(settings, data.IsLand, uv3d, heightNoise);

        // 计算植物等级
        if(data.IsLand)
        {
            data.environmentData.plantLevelType = ResolvePlantLevel(data.environmentData.terrainType, data.environmentData.moistureType);
        }
        else
        {
            data.environmentData.plantLevelType = PlantLevelType.Sparse;
        }

        data.Seed = (int)(heightNoise * 1000000);

        return data;
    }

    public PlantLevelType ResolvePlantLevel(TerrainType terrainTierType, MoistureType moistureBandType)
    {
        // Chance 越高代表地形越适宜植物生长，作为 roll 的倍率使高 Chance 时更容易出现 Dense
        float Chance = 0.25f;
        if (terrainTierType == TerrainType.Plain || terrainTierType == TerrainType.Hills)
            Chance += 0.15f;
        else if (terrainTierType == TerrainType.Highlands || terrainTierType == TerrainType.Valleys)
            Chance += 0.05f;
        // Shore / Snow / Polar：不额外加分

        if (moistureBandType == MoistureType.Wet)
            Chance += 0.20f;
        else
            Chance += 0.10f;

        // Chance ∈ [0.35, 0.60]
        // roll 经 Chance 放大后最大可超过 0.66，使 Dense 在高 Chance 时可触达
        float roll = Random.Range(0f, 1f) * (2f * Chance);
        if (roll < 0.33f)      return PlantLevelType.Sparse;
        else if (roll < 0.66f) return PlantLevelType.Regular;
        else                   return PlantLevelType.Dense;
    }

    /// <summary>
    /// 计算地形类型
    /// </summary>
    private static TerrainType ResolveTerrainTier(
        PlanetGenerator.PlanetSettings settings,
        bool isLand,
        Vector3 uv3d,
        float heightNoise)
    {   
        if (!isLand) 
        {
            return TerrainType.Ocean;
        }

        // 计算本地坐标
        Vector3 localPos = (uv3d - (Vector3.one * 0.5f)) * 2.0f;

        // 计算距离地表的距离
        float distY = Mathf.Abs(localPos.y) + (heightNoise - 0.5f) * 0.5f;

        // 计算是否为极地
        bool isPolar = distY >= settings.poleThreshold;
        if (isPolar) 
        {
            return TerrainType.Polar;
        }

        // 与 Planet.shader 完全一致：
        // landH = (hNoise - seaLevel) / (1 - seaLevel)
        // 再用 landH 去采样 _GradientTexture。
        float invLandRange = 1.0f / Mathf.Max(0.0001f, 1.0f - settings.seaLevel);
        float landH = (heightNoise - settings.seaLevel) * invLandRange;

        // 与 UpdatePlanetGradient 分层顺序一致：
        // Shore → Plain1 → Plain2 → Mountain1 → Mountain2 → Snow
        if (landH < settings.shoreThreshold)     return TerrainType.Shore;
        if (landH < settings.plain1Threshold)    return TerrainType.Plain;
        if (landH < settings.plain2Threshold)    return TerrainType.Hills;
        if (landH < settings.mountain1Threshold) return TerrainType.Highlands;
        if (landH < settings.mountain2Threshold) return TerrainType.Valleys;
        return TerrainType.Snow;
    }
}
