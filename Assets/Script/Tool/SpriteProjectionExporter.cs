using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteProjectionExporter : EditorWindow
{
    private Texture2D sourceTexture;
    private int frameSize = 32;
    private string outputPath = "";
    private string outputFileName = "shadow_merged";

    [MenuItem("Tools/Sprite Projection Exporter")]
    public static void ShowWindow()
    {
        GetWindow<SpriteProjectionExporter>("Sprite Projection Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Projection Exporter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Source Texture", sourceTexture, typeof(Texture2D), false);

        if (sourceTexture != null)
        {
            int autoFrameCount = frameSize > 0 ? sourceTexture.width / frameSize : 0;
            EditorGUILayout.HelpBox(
                $"Texture: {sourceTexture.width} x {sourceTexture.height} | " +
                $"Detected Frames: {autoFrameCount}",
                MessageType.Info);
        }

        frameSize = EditorGUILayout.IntField("Frame Size (Square px)", frameSize);
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Folder", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Output Folder", outputPath, "");
            if (!string.IsNullOrEmpty(selected))
                outputPath = selected;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        bool canExport = sourceTexture != null 
                      && frameSize > 0 
                      && !string.IsNullOrEmpty(outputPath);

        GUI.enabled = canExport;
        if (GUILayout.Button("Export Merged Shadow"))
        {
            ExportMergedShadow();
        }
        GUI.enabled = true;
    }

    private void ExportMergedShadow()
    {
        // 确保纹理可读
        string texPath = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(texPath);
        }

        int frameCount = sourceTexture.width / frameSize;
        // 输出：单张 frameSize x frameSize
        bool[] mask = new bool[frameSize * frameSize]; // true = 有像素

        for (int f = 0; f < frameCount; f++)
        {
            int startX = f * frameSize;
            // 单行，纹理原点左下，帧在底部
            int startY = sourceTexture.height - frameSize;

            Color[] pixels = sourceTexture.GetPixels(startX, startY, frameSize, frameSize);

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0f)
                    mask[i] = true;
            }
        }

        // 生成输出纹理
        Texture2D output = new Texture2D(frameSize, frameSize, TextureFormat.RGBA32, false);
        output.filterMode = FilterMode.Point;

        Color[] outPixels = new Color[frameSize * frameSize];
        for (int i = 0; i < mask.Length; i++)
        {
            outPixels[i] = mask[i] ? Color.white : Color.clear;
        }

        output.SetPixels(outPixels);
        output.Apply();

        // 输出 PNG
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string filePath = Path.Combine(outputPath, $"{outputFileName}.png");
        File.WriteAllBytes(filePath, output.EncodeToPNG());
        DestroyImmediate(output);

        AssetDatabase.Refresh();
        Debug.Log($"[SpriteProjectionExporter] Saved to: {filePath}");
        EditorUtility.DisplayDialog("Done", $"Shadow exported to:\n{filePath}", "OK");
    }
}