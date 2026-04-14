using QFramework.System;
using UnityEngine;
using QFramework.Model;


[System.Serializable]
public class PlanetNodeMapData
{
    public Vector3 SurfaceNormal;
    public Vector3 Uv3D;
    public float HeightNoise;
    public float MoistureNoise;
    public float LandHeight01;
    public int Seed;
    public bool IsLand;
    public bool IsSunlit;
    public PlanetTerrainType TerrainTierType;
    public PlanetMoistureType MoistureBandType;
    public PlantLevelType PlantLevelType;


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
        data.SurfaceNormal = surfaceNormal.normalized;
        data.Uv3D = uv3d;
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
        data.IsSunlit = Vector3.Dot(data.SurfaceNormal, lightDir) >= sunlitDotThreshold; 

        // 计算湿度
        data.MoistureBandType = PlanetMoistureType.Dry;
        if(moistureNoise >= settings.moistureThreshold)
        {
            data.MoistureBandType = PlanetMoistureType.Wet;
        }


        data.TerrainTierType = ResolveTerrainTier(settings, data.IsLand, uv3d, heightNoise);

        // 计算植物等级
        if(data.IsLand)
        {
            data.PlantLevelType = ResolvePlantLevel(data.TerrainTierType, data.MoistureBandType);
        }

        data.Seed = (int)(heightNoise * 1000000);

        return data;
    }

    public PlantLevelType ResolvePlantLevel(PlanetTerrainType terrainTierType, PlanetMoistureType moistureBandType)
    {
        // Chance 越高代表地形越适宜植物生长，作为 roll 的倍率使高 Chance 时更容易出现 Dense
        float Chance = 0.25f;
        if (terrainTierType == PlanetTerrainType.Plain1 || terrainTierType == PlanetTerrainType.Plain2)
            Chance += 0.15f;
        else if (terrainTierType == PlanetTerrainType.Mountain1 || terrainTierType == PlanetTerrainType.Mountain2)
            Chance += 0.05f;
        // Shore / Snow / Polar：不额外加分

        if (moistureBandType == PlanetMoistureType.Wet)
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
    private static PlanetTerrainType ResolveTerrainTier(
        PlanetGenerator.PlanetSettings settings,
        bool isLand,
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

        // 与 Shader UpdatePlanetGradient 保持相同的梯度分层顺序：
        // Shore → Plain1 → Plain2 → Mountain1 → Mountain2 → Snow
        // （Shore 仅在 seaLevel < shoreThreshold 时可见；若不满足则直接跳至 Plain1）

        if (heightNoise - settings.seaLevel < settings.shoreThreshold)    return PlanetTerrainType.Shore;
        if (heightNoise - settings.seaLevel < settings.plain1Threshold)   return PlanetTerrainType.Plain1;
        if (heightNoise - settings.seaLevel < settings.plain2Threshold)   return PlanetTerrainType.Plain2;
        if (heightNoise - settings.seaLevel < settings.mountain1Threshold) return PlanetTerrainType.Mountain1;
        if (heightNoise - settings.seaLevel < settings.mountain2Threshold) return PlanetTerrainType.Mountain2;
        return PlanetTerrainType.Snow;
    }
}
