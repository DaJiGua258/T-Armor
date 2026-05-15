using System.Collections.Generic;
using UnityEngine;

namespace QFramework.UI.Terminal
{
    public struct MatrixData
    {
        public string[,] Grid;      // [row, col], 4x4
        public string[] AnswerCodes; // 4 个答案码
        public int[] AnswerRows;    // 每列答案码所在的行
    }

    public struct PuzzleData
    {
        public string[,] Grid;      // 4x4 网格
        public string[][] CommandCodes; // 3 个指令各自的 4 个答案码
        public string[] Decoys;     // 4 个干扰码
    }

    public static class CodeMatrixGenerator
    {
        private static readonly string[] CodePool = new[]
        {
            "1C", "BD", "E9", "55", "A3", "7F", "2A", "CC",
            "D4", "F8", "3B", "6E", "9A", "0F", "47", "81",
            "B2", "C6"
        };

        // 每列的排列顺序：[C1行, C2行, C3行, D行]
        // 不同列的排列不同，保证任意两指令间的向前循环距离不全相等 → 互斥
        private static readonly int[,] ColumnLayouts = new int[4, 4]
        {
            { 0, 1, 2, 3 }, // Col 0: C1@0, C2@1, C3@2, D@3
            { 0, 2, 1, 3 }, // Col 1: C1@0, C3@1, C2@2, D@3
            { 1, 0, 3, 2 }, // Col 2: C2@0, C1@1, D@2, C3@3
            { 0, 3, 2, 1 }, // Col 3: C1@0, D@1, C3@2, C2@3
        };

        /// <summary>
        /// 生成 4x4 矩阵：4 个答案码分别随机散落到对应列的某一行。
        /// </summary>
        public static MatrixData Generate(string[] answerCodes)
        {
            Debug.Assert(answerCodes != null && answerCodes.Length == 4);

            var grid = new string[4, 4];
            var answerRows = new int[4];

            // 每列随机选一行放答案码
            for (int col = 0; col < 4; col++)
            {
                int answerRow = Random.Range(0, 4);
                answerRows[col] = answerRow;
                grid[answerRow, col] = answerCodes[col];
            }

            // 填充剩余格（干扰码 ≠ 该列答案码）
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (!string.IsNullOrEmpty(grid[row, col])) continue;
                    grid[row, col] = GetDecoy(answerCodes[col]);
                }
            }

            return new MatrixData
            {
                Grid = grid,
                AnswerCodes = answerCodes,
                AnswerRows = answerRows
            };
        }

        /// <summary>
        /// 生成多指令谜题数据：3 个指令各 4 个答案码，每列不同排列保证互斥。
        /// </summary>
        public static PuzzleData GenerateCommands()
        {
            var grid = new string[4, 4];
            var commandCodes = new string[3][];

            // 从代码池中随机选 12 个不重复的码，分成 3 组
            string[] selected = SelectDistinctCodes(12);
            for (int i = 0; i < 3; i++)
            {
                commandCodes[i] = new string[4];
                for (int j = 0; j < 4; j++)
                    commandCodes[i][j] = selected[i * 4 + j];
            }

            // 从剩余池中选 4 个不重复的干扰码
            var usedSet = new HashSet<string>(selected);
            string[] decoys = new string[4];
            for (int c = 0; c < 4; c++)
            {
                decoys[c] = GetDecoyFromPool(usedSet);
                usedSet.Add(decoys[c]);
            }

            // 按排列填充 4x4 网格：每列使用不同的 ColumnLayout
            for (int c = 0; c < 4; c++)
            {
                int rowC1 = ColumnLayouts[c, 0];
                int rowC2 = ColumnLayouts[c, 1];
                int rowC3 = ColumnLayouts[c, 2];
                int rowD  = ColumnLayouts[c, 3];
                grid[rowC1, c] = commandCodes[0][c];
                grid[rowC2, c] = commandCodes[1][c];
                grid[rowC3, c] = commandCodes[2][c];
                grid[rowD,  c] = decoys[c];
            }

            // 对各列进行随机循环移位，打乱初始状态
            for (int c = 0; c < 4; c++)
            {
                int shift = Random.Range(1, 4);
                for (int s = 0; s < shift; s++)
                {
                    string temp = grid[0, c];
                    for (int r = 0; r < 3; r++)
                        grid[r, c] = grid[r + 1, c];
                    grid[3, c] = temp;
                }
            }

            return new PuzzleData
            {
                Grid = grid,
                CommandCodes = commandCodes,
                Decoys = decoys
            };
        }

        private static string[] SelectDistinctCodes(int count)
        {
            var pool = new List<string>(CodePool);
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result[i] = pool[idx];
                pool.RemoveAt(idx);
            }
            return result;
        }

        private static string GetDecoyFromPool(HashSet<string> exclude)
        {
            string result;
            do result = CodePool[Random.Range(0, CodePool.Length)];
            while (exclude.Contains(result));
            return result;
        }

        private static string GetDecoy(string exclude)
        {
            string result;
            do result = CodePool[Random.Range(0, CodePool.Length)];
            while (result == exclude);
            return result;
        }
    }
}
