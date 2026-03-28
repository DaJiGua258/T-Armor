using UnityEngine;

public static class NoiseMapUtil
{
    /// <summary>
    /// 生成归一化到 [0,1] 的 Perlin Noise 噪声图。
    /// </summary>
    public static float[,] Generate(int width, int height, int seed, float scale,
        int octaves, float persistence, float lacunarity)
    {
        float[,] map = new float[width, height];

        if (scale <= 0f) scale = 0.001f;

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float ox = rng.Next(-100000, 100000);
            float oy = rng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(ox, oy);
        }

        float maxVal = float.MinValue;
        float minVal = float.MaxValue;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float value = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sx = (x - halfW) / scale * frequency + octaveOffsets[o].x;
                    float sy = (y - halfH) / scale * frequency + octaveOffsets[o].y;
                    value += Mathf.PerlinNoise(sx, sy) * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                map[x, y] = value;
                if (value > maxVal) maxVal = value;
                if (value < minVal) minVal = value;
            }
        }

        // 归一化到 [0, 1]
        float range = maxVal - minVal;
        if (range < 1e-6f) range = 1e-6f;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[x, y] = (map[x, y] - minVal) / range;

        return map;
    }
}
