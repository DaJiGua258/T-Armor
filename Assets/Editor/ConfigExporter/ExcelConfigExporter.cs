using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExcelDataReader;

public static class ExcelConfigExporter
{
    public static void ExportAll()
    {
        string designDataPath = Path.Combine(Application.dataPath, "..", "DesignData");
        string outputDir = Path.Combine(Application.dataPath, "Resources", "Config");

        if (!Directory.Exists(designDataPath))
        {
            Debug.LogError($"[导出] DesignData 目录不存在: {designDataPath}");
            EditorUtility.DisplayDialog("导出失败", $"DesignData 目录不存在\n{designDataPath}", "确定");
            return;
        }

        Directory.CreateDirectory(outputDir);

        string[] files = Directory.GetFiles(designDataPath, "*.xlsx");
        if (files.Length == 0)
        {
            Debug.LogWarning("[导出] DesignData 下没有 .xlsx 文件");
            EditorUtility.DisplayDialog("导出", "DesignData 下没有 .xlsx 文件", "确定");
            return;
        }

        foreach (string file in files)
            ExportFile(file, outputDir);

        AssetDatabase.Refresh();
        Debug.Log($"[导出] ✅ 完成，共导出 {files.Length} 个文件");
        EditorUtility.DisplayDialog("导出完成", $"共导出 {files.Length} 个文件\n输出: Resources/Config/", "确定");
    }

    private static void ExportFile(string excelPath, string outputDir)
    {
        string fileName = Path.GetFileNameWithoutExtension(excelPath);
        string jsonPath = Path.Combine(outputDir, fileName + ".json");

        using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read))
        using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
        {
            // Row 0: 字段名
            reader.Read();
            int colCount = reader.FieldCount;
            string[] headers = new string[colCount];
            for (int i = 0; i < colCount; i++)
            {
                object val = reader.GetValue(i);
                headers[i] = val?.ToString() ?? $"Col{i}";
            }

            // Row 1: 类型注解
            reader.Read();
            string[] types = new string[colCount];
            for (int i = 0; i < colCount && i < reader.FieldCount; i++)
            {
                object val = reader.GetValue(i);
                types[i] = val?.ToString() ?? "string";
            }

            // Rows 2+: 数据
            var rows = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                bool hasData = false;

                for (int i = 0; i < colCount; i++)
                {
                    object val = reader.GetValue(i);
                    if (val == null) continue;

                    hasData = true;
                    string typeHint = i < types.Length ? types[i] : "string";
                    row[headers[i]] = ConvertValue(val, typeHint);
                }

                if (hasData)
                    rows.Add(row);
            }

            string json = SerializeJson(rows);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);

            Debug.Log($"[导出]   → {fileName}.json ({rows.Count} 行)");
        }
    }

    private static object ConvertValue(object raw, string typeHint)
    {
        // ExcelDataReader 把所有数字都读成 double
        if (typeHint == "int")
        {
            double d = raw is double ? (double)raw : double.Parse(raw.ToString(), CultureInfo.InvariantCulture);
            return (int)d;
        }
        if (typeHint == "float")
        {
            if (raw is double)
                return raw;
            return double.Parse(raw.ToString(), CultureInfo.InvariantCulture);
        }
        // enum:XXX → 转为 int 值（JsonUtility 不认枚举名，只认数字）
        if (typeHint.StartsWith("enum:"))
        {
            string enumName = typeHint.Substring(5);
            var enumType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == enumName && t.IsEnum);
            if (enumType != null)
            {
                string rawStr = raw.ToString();
                try
                {
                    object parsed = System.Enum.Parse(enumType, rawStr);
                    return (int)parsed;
                }
                catch { }
            }
            return raw.ToString();
        }
        return raw.ToString();
    }

    private static string SerializeJson(List<Dictionary<string, object>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"Items\": [");

        for (int r = 0; r < rows.Count; r++)
        {
            sb.Append("    { ");
            int c = 0;
            foreach (var kv in rows[r])
            {
                sb.Append('"').Append(kv.Key).Append("\": ");
                if (kv.Value is string s)
                {
                    sb.Append('"').Append(EscapeJson(s)).Append('"');
                }
                else
                {
                    // float/double → 用不变区域格式，确保小数点不是逗号
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", kv.Value);
                }

                if (++c < rows[r].Count)
                    sb.Append(", ");
            }
            sb.Append(" }");

            if (r < rows.Count - 1)
                sb.Append(',');
            sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
}
