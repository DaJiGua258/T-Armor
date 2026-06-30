using UnityEngine;
using UnityEditor;
using ExcelDataReader;
using System.IO;
using System.Linq;

public class TestExcelReader
{
    [MenuItem("Tools/测试 Excel 读取")]
    public static void TestRead()
    {
        string excelPath = Path.Combine(Application.dataPath, "CSVTable", "TestConfig.xlsx");

        if (!File.Exists(excelPath))
        {
            Debug.LogError($"文件不存在: {excelPath}");
            return;
        }

        Debug.Log($"📂 读取: {excelPath}");

        using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read))
        using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
        {
            int rowIndex = 0;
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                int count = reader.GetValues(values);
                string row = string.Join(" | ", Enumerable.Range(0, count)
                    .Select(i => $"[{i}]={values[i]}"));
                Debug.Log($"Row {rowIndex}: {row}");
                rowIndex++;
            }

            Debug.Log($"✅ 读取完成，共 {rowIndex} 行");
        }
    }
}
