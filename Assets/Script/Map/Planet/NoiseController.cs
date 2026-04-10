using UnityEngine;

[ExecuteAlways]
public class NoiseController : MonoBehaviour
{
    public ComputeShader noiseCompute;
    public Material targetMaterial;

    [Header("Noise Settings")]
    [Range(1f, 50f)] public float scale = 10f;
    public Vector3 offset;
    [Range(1, 8)] public int octaves = 4;

    [Header("Gradient Settings")]
    public Gradient gradient; // Unity 原生漸變色面板
    
    [Header("Texture Settings")]
    public int textureSize = 64; 

    private RenderTexture noiseRT;
    private Texture2D gradientTex; // 烘焙出的 1D 貼圖

    void OnEnable() {
        ValidateAndInit();
    }

    void OnValidate() {
        // 面板參數一動，就重新烘焙貼圖並計算
        BakeGradient();
        ValidateAndInit();
    }

    void Update() {
        if (noiseCompute == null || targetMaterial == null) return;

        if (noiseRT == null || !noiseRT.IsCreated() || gradientTex == null) {
            ValidateAndInit();
        }

        if (Application.isPlaying) {
            DispatchCompute();
        }
    }

    // 將 Gradient 烘焙成 256x1 的貼圖
    void BakeGradient() {
        if (gradient == null) return;
        
        if (gradientTex == null) {
            gradientTex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            gradientTex.wrapMode = TextureWrapMode.Clamp;
            gradientTex.filterMode = FilterMode.Bilinear;
            gradientTex.hideFlags = HideFlags.DontSave;
        }

        Color[] colors = new Color[256];
        for (int i = 0; i < 256; i++) {
            colors[i] = gradient.Evaluate(i / 255f);
        }
        gradientTex.SetPixels(colors);
        gradientTex.Apply();

        if (targetMaterial) {
            targetMaterial.SetTexture("_GradientTex", gradientTex);
        }
    }

    private void ValidateAndInit() {
        if (noiseCompute == null || targetMaterial == null) return;

        if (noiseRT == null || !noiseRT.IsCreated()) {
            CreateRT();
        }
        
        if (gradientTex == null) {
            BakeGradient();
        }

        targetMaterial.SetTexture("_NoiseTex", noiseRT);
        targetMaterial.SetTexture("_GradientTex", gradientTex);
        
        DispatchCompute();
    }

    void CreateRT() {
        if (noiseRT != null) noiseRT.Release();

        noiseRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.RFloat); // 改用單通道 RFloat，更輕量
        noiseRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        noiseRT.volumeDepth = textureSize;
        noiseRT.enableRandomWrite = true;
        noiseRT.Create();
    }

    void DispatchCompute() {
        if (noiseCompute == null || noiseRT == null) return;

        int kernel = noiseCompute.FindKernel("CSMain");
        noiseCompute.SetTexture(kernel, "Result", noiseRT);
        noiseCompute.SetFloat("_Scale", scale);
        
        Vector3 finalOffset = offset;

        
        noiseCompute.SetVector("_Offset", finalOffset);
        noiseCompute.SetInt("_Octaves", octaves);
        noiseCompute.SetFloat("_Persistence", 0.5f);
        noiseCompute.SetFloat("_Lacunarity", 2.0f);

        int groups = Mathf.CeilToInt(textureSize / 8f);
        noiseCompute.Dispatch(kernel, groups, groups, groups);

        #if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(targetMaterial);
        #endif
    }
}