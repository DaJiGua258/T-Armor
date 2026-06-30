using UnityEngine;
using UnityEditor;
using System.IO;

public class ConfigExporterWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private string _designDataPath;
    private string _outputDir;
    private string[] _excelFiles = new string[0];

    [MenuItem("Tools/导出配置表")]
    private static void Open()
    {
        var win = GetWindow<ConfigExporterWindow>();
        win.titleContent = new GUIContent("导出配置表");
        win.minSize = new Vector2(400, 300);
        win.RefreshFileList();
        win.Show();
    }

    private void OnEnable()
    {
        _designDataPath = Path.Combine(Application.dataPath, "..", "DesignData");
        _outputDir = Path.Combine(Application.dataPath, "Resources", "Config");
        RefreshFileList();
    }

    private void RefreshFileList()
    {
        if (Directory.Exists(_designDataPath))
            _excelFiles = Directory.GetFiles(_designDataPath, "*.xlsx");
        else
            _excelFiles = new string[0];
    }

    private void OnGUI()
    {
        GUILayout.Space(8);

        // 标题
        EditorGUILayout.LabelField("配置表导出工具", EditorStyles.boldLabel);
        GUILayout.Space(4);

        // 目录信息
        EditorGUILayout.LabelField("Excel 源文件:", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(_designDataPath, GUILayout.Height(18));
        GUILayout.Space(2);
        EditorGUILayout.LabelField("JSON 输出:", EditorStyles.miniLabel);
        EditorGUILayout.SelectableLabel(_outputDir, GUILayout.Height(18));

        GUILayout.Space(8);

        // 文件列表
        EditorGUILayout.LabelField($"待导出文件 ({_excelFiles.Length})", EditorStyles.boldLabel);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));
        if (_excelFiles.Length == 0)
        {
            EditorGUILayout.HelpBox("DesignData 目录下没有 .xlsx 文件", MessageType.Info);
        }
        else
        {
            foreach (string file in _excelFiles)
            {
                string name = Path.GetFileName(file);
                EditorGUILayout.LabelField("📄 " + name);
            }
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(12);

        // 按钮
        GUI.enabled = _excelFiles.Length > 0;
        if (GUILayout.Button("一键导出", GUILayout.Height(36)))
            ExcelConfigExporter.ExportAll();
        GUI.enabled = true;

        GUILayout.Space(4);

        if (GUILayout.Button("打开 DesignData 目录"))
        {
            if (!Directory.Exists(_designDataPath))
                Directory.CreateDirectory(_designDataPath);
            EditorUtility.RevealInFinder(_designDataPath);
        }
    }
}
