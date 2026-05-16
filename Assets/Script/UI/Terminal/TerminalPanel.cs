using System;
using System.Collections;
using QFramework.Manager;
using QFramework.UI.Terminal;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.ViewController.UI
{
    [Serializable]
    public struct CommandEntry
    {
        public string Name;          // 指令名称，构造函数自动加 > 前缀
        public string[] AnswerCodes; // 4个答案码（每列一个），由 GeneratePuzzle 填充
        public Action Callback;      // 执行的回调

        public CommandEntry(string name, Action callback)
        {
            Name = name;
            AnswerCodes = null;
            Callback = callback;
        }
    }

    public class TerminalPanel : AbstractBasePanel
    {
        // 组件引用
        [SerializeField] private Text _answerText;
        [SerializeField] private Text _commandListText;
        [SerializeField] private GridLayoutGroup _cellContainer;

        // 运行时状态
        private MatrixData _matrix;
        private int _currentCol;               // 当前选中的列 (0-3)
        private Button[] _cells;               // 16 个格子 [row * 4 + col]
        private Text[] _cellLabels;
        private Image[] _cellImages;           // 格子背景图
        private Sprite[] _defaultSprites;      // 格子默认 Sprite（用于恢复）

        private CommandEntry[] _commands;      // 3 个指令（外部传入）
        private bool[] _commandTriggered;      // 标记指令是否已被触发
        private bool _isBooting;               // 开机协程运行中
        private CommandEntry[] _pendingCommands; // 待显示的指令（OnShow 时消费）
        private int _postCmdLineCount;         // 指令列表输出后累积行数，用于逐行上移

        private static readonly Color DimColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color BrightColor = Color.white;

        public override void OnInit()
        {
            int childCount = _cellContainer.transform.childCount;
            _cells = new Button[childCount];
            _cellLabels = new Text[childCount];
            _cellImages = new Image[childCount];
            _defaultSprites = new Sprite[childCount];
            for (int i = 0; i < childCount; i++)
            {
                var child = _cellContainer.transform.GetChild(i);
                _cells[i] = child.GetComponent<Button>();
                _cellLabels[i] = child.GetComponentInChildren<Text>();
                _cellImages[i] = child.GetComponent<Image>();
                _defaultSprites[i] = _cellImages[i].sprite;
            }
        }

        public override void OnShow()
        {
            _currentCol = 0;

            if (_pendingCommands != null)
            {
                _commands = _pendingCommands;
                _pendingCommands = null;
                _commandTriggered = new bool[3];
                _isBooting = true;
                _commandListText.text = "";
                StartCoroutine(BootSequence());
            }
        }

        /// <summary>
        /// 设置指令并通过 UIGameManager.ShowPanel 显示（走正常面板流程，含 camera 复位等）。
        /// </summary>
        public void ShowWithCommands(CommandEntry cmd1, CommandEntry cmd2, CommandEntry cmd3)
        {
            _pendingCommands = new CommandEntry[] { cmd1, cmd2, cmd3 };
            UIGameManager.Instance.ShowPanel(UIGamePanelType.TerminalPanel);
        }

        [ContextMenu("Test Commands")]
        public void TestCommands()
        {
            ShowWithCommands(
                new CommandEntry("开启传送门", () => Debug.Log("传送门已开启")),
                new CommandEntry("", null),
                new CommandEntry("", null)
            );
        }

        private static readonly WaitForSeconds BootDelay = new WaitForSeconds(0.1f);

        private IEnumerator BootSequence()
        {
            string[] lines = new string[]
            {
                "> 系统初始化...",
                "> 已检测到[]登入...已登入",
                ">...",
                "> 按下 F 退出终端",
                "> 右侧输入对应的密码执行指令",
                "> ..."
            };

            for (int i = 0; i < lines.Length; i++)
            {
                _commandListText.text += lines[i] + "\n";
                if(i == 0) yield return new WaitForSeconds(1f);
                if(i == 2) yield return new WaitForSeconds(0.5f);
                yield return BootDelay;
            }

            GeneratePuzzle();
            PopulateGrid();
            RefreshCommandList();
            _postCmdLineCount = 0;
            AppendPostCommandText("> ...\n");
            RefreshVisual();
            RefreshHighlights();

            _isBooting = false;
        }

        void Update()
        {
            if (!gameObject.activeInHierarchy)
                return;

            // F 键随时退出终端（包括开机动画期间）
            if (Input.GetKeyDown(KeyCode.F))
            {
                UIGameManager.Instance.HidePanel(UIGamePanelType.TerminalPanel);
                return;
            }

            if (_isBooting || _matrix.Grid == null) return;

            // 所有非空指令都已触发则停止输入（仅禁方向键）
            bool allDone = true;
            for (int i = 0; i < _commands.Length; i++)
            {
                if (_commands[i].Callback != null && !_commandTriggered[i])
                {
                    allDone = false;
                    break;
                }
            }
            if (allDone) return;

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _currentCol = (_currentCol + 1) % 4;
                RefreshVisual();
                RefreshHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _currentCol = (_currentCol + 3) % 4;
                RefreshVisual();
                RefreshHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                ShiftColumn(_currentCol, -1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                ShiftColumn(_currentCol, 1);
            }
        }

        /// <summary>
        /// 将当前列的 4 个代码整体循环移位。
        /// </summary>
        private void ShiftColumn(int col, int direction)
        {
            string[] temp = new string[4];
            for (int r = 0; r < 4; r++)
                temp[r] = _matrix.Grid[r, col];

            for (int r = 0; r < 4; r++)
            {
                int newRow = (r + direction + 4) % 4;
                _matrix.Grid[newRow, col] = temp[r];
            }

            for (int r = 0; r < 4; r++)
            {
                int idx = r * 4 + col;
                if (idx < _cellLabels.Length)
                    _cellLabels[idx].text = _matrix.Grid[r, col];
            }

            RefreshVisual();
            RefreshHighlights();
            CheckCommands();
        }

        private void GeneratePuzzle()
        {
            PuzzleData puzzle = CodeMatrixGenerator.GenerateCommands();

            for (int i = 0; i < 3; i++)
            {
                var cmd = _commands[i];
                cmd.AnswerCodes = new string[4];
                Array.Copy(puzzle.CommandCodes[i], cmd.AnswerCodes, 4);
                _commands[i] = cmd;
            }

            _matrix = new MatrixData
            {
                Grid = puzzle.Grid,
                AnswerCodes = puzzle.CommandCodes[0],
                AnswerRows = new int[4]
            };

            _answerText.text = "破解终端";
        }

        private void PopulateGrid()
        {
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int idx = r * 4 + c;
                    if (idx >= _cells.Length) break;
                    _cellLabels[idx].text = _matrix.Grid[r, c];
                }
            }
        }

        private void RefreshCommandList()
        {
            string text = "> 检测到可使用的命令：\n";
            for (int i = 0; i < _commands.Length; i++)
            {
                var cmd = _commands[i];
                string name = string.IsNullOrEmpty(cmd.Name) || cmd.Callback == null ? "NULL" : cmd.Name;
                string codes = cmd.AnswerCodes != null
                    ? string.Join(", ", cmd.AnswerCodes)
                    : "NULL";
                text += $"> [{i + 1}] {name} [{codes}]\n";
            }
            _commandListText.text += text;
        }

        /// <summary>
        /// 列高亮：当前选中列 color=白色，其余列 color=灰色 0.6。
        /// </summary>
        private void RefreshVisual()
        {
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int idx = r * 4 + c;
                    if (idx >= _cellImages.Length) continue;
                    bool isSelected = c == _currentCol;
                    _cellImages[idx].color = isSelected ? BrightColor : DimColor;
                    _cellLabels[idx].color = isSelected ? BrightColor : DimColor;
                }
            }
        }

        /// <summary>
        /// 满足高亮：匹配指令答案码的格子 sprite=null + text=black（UIHighlight 模式）。
        /// 不匹配的格子只恢复 sprite，不碰 color（保留列高亮结果）。
        /// </summary>
        private void RefreshHighlights()
        {
            // 先全部恢复默认 sprite
            for (int i = 0; i < _cellImages.Length; i++)
                _cellImages[i].sprite = _defaultSprites[i];

            for (int i = 0; i < _commands.Length; i++)
            {
                if (_commandTriggered[i]) continue;
                if (_commands[i].Callback == null) continue;
                if (_commands[i].AnswerCodes == null) continue;

                // 找该指令第 0 列答案码所在行（目标行）
                int targetRow = -1;
                for (int r = 0; r < 4; r++)
                {
                    if (_matrix.Grid[r, 0] == _commands[i].AnswerCodes[0])
                    {
                        targetRow = r;
                        break;
                    }
                }
                if (targetRow < 0) continue;

                // 逐列检查是否匹配，匹配则 sprite=null + text=black（覆写列高亮）
                for (int c = 0; c < 4; c++)
                {
                    if (_matrix.Grid[targetRow, c] == _commands[i].AnswerCodes[c])
                    {
                        int idx = targetRow * 4 + c;
                        if (idx < _cellImages.Length)
                        {
                            _cellImages[idx].sprite = null;
                            _cellLabels[idx].color = Color.black;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 指令列表输出后追加文本，每输出一行将 _commandListText 的 Y 轴上移 20。
        /// </summary>
        private void AppendPostCommandText(string text)
        {
            _commandListText.text += text;

            int lineCount = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') lineCount++;

            _postCmdLineCount += lineCount;
            var pos = _commandListText.rectTransform.anchoredPosition;
            pos.y += lineCount * 20;
            _commandListText.rectTransform.anchoredPosition = pos;
        }

        private void CheckCommands()
        {
            for (int i = 0; i < _commands.Length; i++)
            {
                if (_commandTriggered[i]) continue;
                if (_commands[i].Callback == null) continue;
                if (_commands[i].AnswerCodes == null) continue;

                int targetRow = -1;

                for (int r = 0; r < 4; r++)
                {
                    if (_matrix.Grid[r, 0] == _commands[i].AnswerCodes[0])
                    {
                        targetRow = r;
                        break;
                    }
                }

                if (targetRow < 0) continue;

                bool solved = true;
                for (int c = 0; c < 4; c++)
                {
                    if (_matrix.Grid[targetRow, c] != _commands[i].AnswerCodes[c])
                    {
                        solved = false;
                        break;
                    }
                }

                if (solved)
                {
                    _commandTriggered[i] = true;

                    // 添加反馈文本
                    string codes = string.Join(", ", _commands[i].AnswerCodes);
                    string name = _commands[i].Name;
                    AppendPostCommandText($"> 已输入命令 [{codes}]\n> 执行：[{name}]\n");

                    _commands[i].Callback?.Invoke();
                }
            }
        }
    }
}
