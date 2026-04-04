using UnityEngine;

/// <summary>
/// 静态噪声工具类，提供多倍频 Perlin 噪声、边缘衰减图及组合接口。
/// </summary>
public static class NoiseUtility
{
    /// <summary>
    /// 生成归一化到 [0,1] 的多倍频 Perlin 噪声图。
    /// </summary>
    public static float[,] GenerateNoiseMap(
        int width, int height,
        float scale,
        int octaves,
        float persistence,
        float lacunarity,
        int seed,
        Vector2 offset)
    {
        if (scale <= 0f)   scale    = 0.0001f;
        if (octaves < 1)   octaves  = 1;

        float[,] map = new float[width, height];
        var rng = new System.Random(seed);

        var octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(
                rng.Next(-100000, 100000) + offset.x,
                rng.Next(-100000, 100000) + offset.y
            );
        }

        float maxNoise = float.MinValue;
        float minNoise = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float value     = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sx = (x - width  * 0.5f) / scale * frequency + octaveOffsets[i].x;
                    float sy = (y - height * 0.5f) / scale * frequency + octaveOffsets[i].y;
                    value += (Mathf.PerlinNoise(sx, sy) * 2f - 1f) * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                map[x, y] = value;
                if (value > maxNoise) maxNoise = value;
                if (value < minNoise) minNoise = value;
            }
        }

        // 归一化到 [0, 1]
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[x, y] = Mathf.InverseLerp(minNoise, maxNoise, map[x, y]);

        return map;
    }

    /// <summary>
    /// 生成边缘衰减图：中心趋近 0，边缘趋近 1。
    /// 叠加到噪声图后可自然封闭地图四周。
    /// </summary>
    public static float[,] GenerateFalloffMap(int width, int height, AnimationCurve curve)
    {
        float[,] map = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float fx = x / (float)width  * 2f - 1f;
                float fy = y / (float)height * 2f - 1f;
                float v  = Mathf.Max(Mathf.Abs(fx), Mathf.Abs(fy));  // 计算距离中心点的距离
                map[x, y] = v * curve.Evaluate(v);
            }
        }

        return map;
    }

    /// <summary>
    /// 将衰减图从噪声图中减去，结果截断到 [0, 1]。
    /// </summary>
    public static float[,] ApplyFalloff(float[,] noiseMap, float[,] falloffMap)
    {
        int w = noiseMap.GetLength(0);
        int h = noiseMap.GetLength(1);
        float[,] result = new float[w, h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result[x, y] = Mathf.Clamp01(noiseMap[x, y] + falloffMap[x, y]);

        return result;
    }


    // S 曲线：中心平缓，边缘陡峭
    private static float FalloffCurve(float t)
    {
        const float a = 3f;
        const float b = 2.2f;
        float ta = Mathf.Pow(t, a);
        float ba = Mathf.Pow(b - b * t, a);
        return ta / (ta + ba);
    }
}
