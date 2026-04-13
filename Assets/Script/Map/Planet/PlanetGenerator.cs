using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class PlanetGenerator : MonoBehaviour
{
    [System.Serializable]
    public class NoiseLayer {
        public string label = "Noise Layer";
        public int seed; // 种子系统
        public float scale = 1;
        public Vector3 scale3D = Vector3.one;
        [Range(1, 8)] public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2.0f;
        public Vector3 offset;
        [HideInInspector] public RenderTexture noiseTex;

        public void Release() {
            if (noiseTex != null) { noiseTex.Release(); noiseTex = null; }
        }
    }

    [System.Serializable]
    public class PlanetSettings {
        [Header("1. Base Terrain (Height)")]
        public NoiseLayer heightNoise;
        public Color seaColor = new Color(0, 0.2f, 0.5f);
        [Range(0, 1)] public float seaLevel = 0.3f;

        [Header("Ground Tiers (Baked to 1D Texture)")]
        public Color shoreColor = Color.yellow;
        [Range(0, 1)] public float shoreThreshold = 0.1f;
        public Color plain1Color = new Color(0.2f, 0.6f, 0.2f);
        [Range(0, 1)] public float plain1Threshold = 0.3f;
        public Color plain2Color = new Color(0.1f, 0.4f, 0.1f);
        [Range(0, 1)] public float plain2Threshold = 0.5f;
        public Color mountain1Color = new Color(0.4f, 0.3f, 0.2f);
        [Range(0, 1)] public float mountain1Threshold = 0.7f;
        public Color mountain2Color = new Color(0.3f, 0.2f, 0.1f);
        [Range(0, 1)] public float mountain2Threshold = 0.9f;
        public Color snowColor = Color.white;

        [Header("2. Climate (Moisture)")]
        public NoiseLayer moistureNoise;
        public Color dryColor = new Color(0.8f, 0.7f, 0.4f);
        public Color wetColor = new Color(0.2f, 0.5f, 0.2f);
        [Range(0, 1)] public float moistureThreshold = 0.5f;
        [Range(0, 1)] public float climateMixStrength = 0.5f;

        [Header("3. Poles")]
        public Color poleColor = Color.white;
        [Range(0, 1)] public float poleThreshold = 0.8f;
        [Range(0, 1)] public float poleStrength = 1.0f;
        [Header("4. Night Color")]
        public Color seaColorNight = Color.white;
        public Color landColorNight = Color.white;

        [HideInInspector] public Texture2D gradientTex;
    }

    [System.Serializable]
    public class CloudSettings {
        public NoiseLayer noise;
        public Gradient alphaGradient;
        [HideInInspector] public Texture2D gradientTex;
        [Header("基本参数")]
        public float cloudHeight = 1;
        public float cloudClip = 0.5f;
        public float CloudSpeed;
        public Vector3 CloudDir;

        [Header("云层生成")]
        public float cloudCoverage = 0.5f;
        public float cloudSoftness = 0.1f;
        public float cloudScale2 = 2.0f;


    }

    public ComputeShader noiseCompute;
    [Range(32, 512)] public int resolution = 64;
    [Tooltip("与 Planet.shader 的 input.positionOS 量纲保持一致。Unity 默认 Sphere 顶点半径通常约为 0.5。")]
    [Range(0.1f, 1.0f)] public float noiseSampleRadiusOS = 0.5f;
    public PlanetSettings planet;
    public Material planetMaterial;
    public CloudSettings clouds;
    public Material cloudMaterial;
    
    private float[] _heightNoiseCache;
    private float[] _moistureNoiseCache;
    private int _noiseCacheResolution = -1;

    private void OnValidate() => Generate();
    private void OnDisable() {
        planet.heightNoise.Release();
        planet.moistureNoise.Release();
        clouds.noise.Release();
        if (planet.gradientTex) DestroyImmediate(planet.gradientTex);
        if (clouds.gradientTex) DestroyImmediate(clouds.gradientTex);
    }

    [ContextMenu("Generate")]
    public void Generate() {
        if (!noiseCompute) return;

        UpdateNoise(planet.heightNoise);
        UpdateNoise(planet.moistureNoise);
        UpdatePlanetGradient(); // 使用新的分層烘焙邏輯
        SyncPlanetMaterial();

        UpdateNoise(clouds.noise);
        UpdateGradient(ref clouds.gradientTex, clouds.alphaGradient);
        SyncCloudMaterial();
    }

    public bool BuildNoiseCacheSync()
    {
        if (planet.heightNoise.noiseTex == null || planet.moistureNoise.noiseTex == null)
        {
            Debug.LogWarning("PlanetGenerator: 噪声纹理不存在，先调用 Generate().");
            return false;
        }

        int totalCount = resolution * resolution * resolution;
        if (_heightNoiseCache == null || _heightNoiseCache.Length != totalCount)
            _heightNoiseCache = new float[totalCount];
        if (_moistureNoiseCache == null || _moistureNoiseCache.Length != totalCount)
            _moistureNoiseCache = new float[totalCount];

        if (!CopyNoiseToBufferSync(planet.heightNoise.noiseTex, _heightNoiseCache, "Height")) return false;
        if (!CopyNoiseToBufferSync(planet.moistureNoise.noiseTex, _moistureNoiseCache, "Moisture")) return false;

        _noiseCacheResolution = resolution;
        return true;
    }

    public global::PlanetNodeMapData EvaluateNodeMapData(Vector3 surfaceNormalWorld, Vector3 mainLightDirection, float sunlitDotThreshold)
    {
        if (_noiseCacheResolution != resolution || _heightNoiseCache == null || _moistureNoiseCache == null)
        {
            bool ok = BuildNoiseCacheSync();
            if (!ok)
            {
                return global::PlanetNodeMapData.Create(
                    planet,
                    surfaceNormalWorld.normalized,
                    Vector3.one * 0.5f,
                    0.0f,
                    0.0f,
                    mainLightDirection,
                    sunlitDotThreshold);
            }
        }

        Vector3 localNormal = transform.InverseTransformDirection(surfaceNormalWorld.normalized).normalized;
        Vector3 localPosOnSurface = localNormal * noiseSampleRadiusOS;
        Vector3 uv3d = localPosOnSurface * 0.5f + (Vector3.one * 0.5f);

        float hNoise = SampleNoise(_heightNoiseCache, uv3d);
        float mNoise = SampleNoise(_moistureNoiseCache, uv3d);

        return global::PlanetNodeMapData.Create(
            planet,
            surfaceNormalWorld.normalized,
            uv3d,
            hNoise,
            mNoise,
            mainLightDirection,
            sunlitDotThreshold);
    }

    void UpdateNoise(NoiseLayer layer) {
        if (layer.noiseTex == null || layer.noiseTex.width != resolution) {
            layer.Release();
            layer.noiseTex = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R32_SFloat);
            layer.noiseTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            layer.noiseTex.volumeDepth = resolution;
            layer.noiseTex.enableRandomWrite = true;
            layer.noiseTex.filterMode = FilterMode.Point;
            layer.noiseTex.wrapMode = TextureWrapMode.Mirror;
            layer.noiseTex.Create();
        }

        int kernel = noiseCompute.FindKernel("CSMain");
        
        // 种子偏移计算
        Vector3 seedOffset = new Vector3(layer.seed * 131.1f % 1000, layer.seed * 633.7f % 1000, layer.seed * 915.2f % 1000);

        noiseCompute.SetInt("_Resolution", resolution);
        noiseCompute.SetVector("_Scale3D", layer.scale3D * layer.scale);
        noiseCompute.SetInt("_Octaves", layer.octaves);
        noiseCompute.SetFloat("_Persistence", layer.persistence);
        noiseCompute.SetFloat("_Lacunarity", layer.lacunarity);
        noiseCompute.SetVector("_Offset", layer.offset + seedOffset);
        noiseCompute.SetTexture(kernel, "_Result", layer.noiseTex);

        int groups = Mathf.CeilToInt(resolution / 8.0f);
        noiseCompute.Dispatch(kernel, groups, groups, groups);
    }

    bool CopyNoiseToBufferSync(RenderTexture source, float[] target, string label)
    {
        int kernel = noiseCompute.FindKernel("CSCopy3DToBuffer");
        int count = resolution * resolution * resolution;
        ComputeBuffer buffer = null;
        try
        {
            buffer = new ComputeBuffer(count, sizeof(float));
            noiseCompute.SetInt("_Resolution", resolution);
            noiseCompute.SetTexture(kernel, "_SourceTex", source);
            noiseCompute.SetBuffer(kernel, "_OutBuffer", buffer);

            int groups = Mathf.CeilToInt(resolution / 8.0f);
            noiseCompute.Dispatch(kernel, groups, groups, groups);

            buffer.GetData(target, 0, 0, count);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PlanetGenerator: ComputeBuffer 同步复制失败 -> {label} ({source.name})\n{e}");
            return false;
        }
        finally
        {
            if (buffer != null) buffer.Release();
        }

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            float value = target[i];
            if (value < min) min = value;
            if (value > max) max = value;
        }

        Debug.Log($"PlanetGenerator: {label} 噪声范围 = [{min:F3}, {max:F3}], SeaLevel = {planet.seaLevel:F3}");

        if (max <= 0.0001f && min <= 0.0001f)
        {
            Debug.LogWarning($"PlanetGenerator: {label} 读回值接近全0，可能导致无法判定陆地。");
            return false;
        }

        // 关键检查：如果最小值大于海平面，所有位置都会被判断为陆地！
        if (min >= planet.seaLevel)
        {
            Debug.LogWarning($"PlanetGenerator: {label} 最小噪声值({min:F3}) >= 海平面({planet.seaLevel:F3})，所有位置都会被判定为陆地！请调整噪声参数或降低海平面。");
        }

        return true;
    }

    float SampleNoise(float[] data, Vector3 uv3d)
    {
        float x = Mathf.Clamp01(uv3d.x) * (resolution - 1);
        float y = Mathf.Clamp01(uv3d.y) * (resolution - 1);
        float z = Mathf.Clamp01(uv3d.z) * (resolution - 1);

        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, resolution - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, resolution - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(z), 0, resolution - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, resolution - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, resolution - 1);
        int z1 = Mathf.Clamp(z0 + 1, 0, resolution - 1);

        float fx = x - x0;
        float fy = y - y0;
        float fz = z - z0;

        float v000 = GetNoiseAt(data, x0, y0, z0);
        float v100 = GetNoiseAt(data, x1, y0, z0);
        float v010 = GetNoiseAt(data, x0, y1, z0);
        float v110 = GetNoiseAt(data, x1, y1, z0);
        float v001 = GetNoiseAt(data, x0, y0, z1);
        float v101 = GetNoiseAt(data, x1, y0, z1);
        float v011 = GetNoiseAt(data, x0, y1, z1);
        float v111 = GetNoiseAt(data, x1, y1, z1);

        float v00 = Mathf.Lerp(v000, v100, fx);
        float v10 = Mathf.Lerp(v010, v110, fx);
        float v01 = Mathf.Lerp(v001, v101, fx);
        float v11 = Mathf.Lerp(v011, v111, fx);

        float v0 = Mathf.Lerp(v00, v10, fy);
        float v1 = Mathf.Lerp(v01, v11, fy);

        return Mathf.Lerp(v0, v1, fz);
    }

    float GetNoiseAt(float[] data, int x, int y, int z)
    {
        int idx = x + y * resolution + z * resolution * resolution;
        if (idx < 0 || idx >= data.Length) return 0.0f;
        return data[idx];
    }

    // 核心優化：將 6 個層級烘焙進 1D 紋理
    void UpdatePlanetGradient() {
        if (planet.gradientTex == null) {
            planet.gradientTex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            planet.gradientTex.wrapMode = TextureWrapMode.Clamp;
            planet.gradientTex.filterMode = FilterMode.Point; // 確保硬邊緣
        }

        for (int i = 0; i < 256; i++) {
            float t = i / 255f;
            Color c;
            if (t < planet.shoreThreshold) c = planet.shoreColor;
            else if (t < planet.plain1Threshold) c = planet.plain1Color;
            else if (t < planet.plain2Threshold) c = planet.plain2Color;
            else if (t < planet.mountain1Threshold) c = planet.mountain1Color;
            else if (t < planet.mountain2Threshold) c = planet.mountain2Color;
            else c = planet.snowColor;

            planet.gradientTex.SetPixel(i, 0, c);
        }
        planet.gradientTex.Apply();
    }

    void UpdateGradient(ref Texture2D tex, Gradient grad) {
        if (tex == null) {
            tex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
        }
        for (int i = 0; i < 256; i++) tex.SetPixel(i, 0, grad.Evaluate(i / 255f));
        tex.Apply();
    }

    void SyncPlanetMaterial() {
        if (!planetMaterial) return;
        planetMaterial.SetTexture("_HeightNoise", planet.heightNoise.noiseTex);
        planetMaterial.SetTexture("_MoistureNoise", planet.moistureNoise.noiseTex);
        planetMaterial.SetTexture("_GradientTexture", planet.gradientTex);
        planetMaterial.SetColor("_SeaColor", planet.seaColor);
        planetMaterial.SetFloat("_SeaLevel", planet.seaLevel);
        planetMaterial.SetColor("_DryColor", planet.dryColor);
        planetMaterial.SetColor("_WetColor", planet.wetColor);
        planetMaterial.SetFloat("_MoistureThreshold", planet.moistureThreshold);
        planetMaterial.SetFloat("_ClimateMixStrength", planet.climateMixStrength);
        planetMaterial.SetColor("_PoleColor", planet.poleColor);
        planetMaterial.SetFloat("_PoleThreshold", planet.poleThreshold);
        planetMaterial.SetFloat("_PoleStrength", planet.poleStrength);

        planetMaterial.SetColor("_SeaColorNight", planet.seaColorNight);
        planetMaterial.SetColor("_LandColorNight", planet.landColorNight);
    }

    void SyncCloudMaterial() 
    {
        if (!cloudMaterial) return;

        // 基础纹理
        cloudMaterial.SetTexture("_NoiseTexture", clouds.noise.noiseTex);
        cloudMaterial.SetTexture("_GradientTexture", clouds.gradientTex);

        // 参数控制
        cloudMaterial.SetFloat("_CloudHeight", clouds.cloudHeight);
        cloudMaterial.SetFloat("_CloudClip", clouds.cloudClip);
        
        // 补充参数 (你可以根据需要在 CloudSettings 类里添加这些变量)
        cloudMaterial.SetFloat("_CloudSpeed", 0.02f); // 云层移动速度
        cloudMaterial.SetFloat("_CloudScale2", 2.0f); // 第二层噪声的缩放，增加细节
        
        // 关键：将星球的陆地颜色传给云层，用于背光面显示
        // 假设使用 planetSettings 里的 plain1Color 作为背光参考
        cloudMaterial.SetColor("_LandColor", planet.plain1Color);
        cloudMaterial.SetFloat("_CloudSpeed", clouds.CloudSpeed);
        cloudMaterial.SetVector("_CloudDir", clouds.CloudDir);

        cloudMaterial.SetColor("_LandColor", planet.landColorNight);

        cloudMaterial.SetFloat("_CloudCoverage", clouds.cloudCoverage);
        cloudMaterial.SetFloat("_CloudSoftness", clouds.cloudSoftness);
        cloudMaterial.SetFloat("_CloudScale2", clouds.cloudScale2);
    }
}