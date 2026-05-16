using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UtilityKit
{
    /// <summary>
    /// Text 文本框自动换行组件。
    /// 同时支持按文本框像素宽度换行和按指定字符数换行。
    /// 挂载到有 Text 组件的 GameObject 上使用。
    /// </summary>
    [RequireComponent(typeof(Text))]
    [ExecuteAlways]
    public class TextWrapper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("超过该字符数后自动换行（0 = 仅根据宽度）")]
        private int _maxCharsPerLine = 0;

        private Text _text;
        private TextGenerator _gen;
        private RectTransform _rect;
        private string _rawText;
        private bool _isWrapping;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _rect = _text.rectTransform;
            _gen = new TextGenerator();
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.RegisterDirtyVerticesCallback(OnTextDirty);
        }

        private void OnDestroy()
        {
            if (_text != null)
                _text.UnregisterDirtyVerticesCallback(OnTextDirty);
        }

        private void OnTextDirty()
        {
            if (_isWrapping || _text == null) return;
            _rawText = _text.text;
            Refresh();
        }

        /// <summary>设置原始文本并自动换行</summary>
        public void SetText(string text)
        {
            _rawText = text;
            Refresh();
        }

        /// <summary>重新应用换行（文本框尺寸变化后调用）</summary>
        public void Refresh()
        {
            if (string.IsNullOrEmpty(_rawText) || _text == null) return;

            _isWrapping = true;
            var settings = _text.GetGenerationSettings(_rect.rect.size);
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.text = Wrap(_rawText, _gen, settings, _rect.rect.width, _maxCharsPerLine);
            _isWrapping = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            Refresh();
        }

        [ContextMenu("Test Wrap")]
        private void TestWrap()
        {
            if (_text == null && !TryGetComponent(out _text))
            {
                Debug.LogWarning("TextWrapper: 找不到 Text 组件");
                return;
            }
            if (_rect == null) _rect = _text.rectTransform;
            if (_gen == null) _gen = new TextGenerator();
            if (_text.horizontalOverflow != HorizontalWrapMode.Overflow)
                _text.horizontalOverflow = HorizontalWrapMode.Overflow;

            SetText(" > 将密钥插入节点电脑的读取槽取槽取槽 (1/1) \n > 登入终端写入访问 (0/1)");
        }

        /// <summary>
        /// 对文本进行自动换行处理。
        /// </summary>
        /// <param name="input">原始文本</param>
        /// <param name="gen">TextGenerator 实例</param>
        /// <param name="settings">文本生成设置（推荐关闭自动换行）</param>
        /// <param name="boxWidth">文本框像素宽度</param>
        /// <param name="maxPerLine">超过该字符数后自动换行（0=不限制）</param>
        public static string Wrap(string input, TextGenerator gen,
            TextGenerationSettings settings, float boxWidth, int maxPerLine)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (gen == null || settings.font == null) return input;

            var paragraphs = input.Split('\n');
            var lines = new List<string>();

            foreach (var para in paragraphs)
            {
                if (string.IsNullOrEmpty(para))
                {
                    lines.Add("");
                    continue;
                }

                var tokens = Tokenize(para);
                var cur = new StringBuilder();

                foreach (var token in tokens)
                {
                    var candidate = cur.Length == 0 ? token : $"{cur} {token}";
                    bool overWidth = gen.GetPreferredWidth(candidate, settings) > boxWidth;
                    bool overMax = maxPerLine > 0 && candidate.Length > maxPerLine;

                    if ((overWidth || overMax) && cur.Length > 0 && cur.ToString().Trim() != ">")
                    {
                        lines.Add(cur.ToString());
                        cur.Clear();
                        cur.Append(token);
                    }
                    else if (overWidth || overMax)
                    {
                        var prefix = cur.Length == 0 ? "" : $"{cur} ";
                        var filledLines = FillChars(prefix, token, gen, settings, boxWidth, maxPerLine, out var rem);
                        lines.AddRange(filledLines);
                        cur.Clear();
                        if (!string.IsNullOrEmpty(rem))
                            cur.Append(rem);
                    }
                    else
                    {
                        cur.Clear();
                        cur.Append(candidate);
                    }
                }

                if (cur.Length > 0)
                    lines.Add(cur.ToString());
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// 逐字填充行：从 prefix 开始，逐个添加 text 中的字符，
        /// 超出宽度或最大字符数时换行。用于 token 内部断行。
        /// </summary>
        /// <returns>已满的行</returns>
        private static List<string> FillChars(string prefix, string text,
            TextGenerator gen, TextGenerationSettings settings, float boxWidth, int maxPerLine,
            out string remainder)
        {
            var filled = new List<string>();
            var cur = new StringBuilder(prefix);
            int addedFromText = 0;

            foreach (char ch in text)
            {
                cur.Append(ch);
                addedFromText++;
                bool overWidth = gen.GetPreferredWidth(cur.ToString(), settings) > boxWidth;
                bool overMax = maxPerLine > 0 && cur.Length > maxPerLine;

                if (overWidth || overMax)
                {
                    cur.Length--;
                    addedFromText--;
                    if (addedFromText > 0)
                        filled.Add(cur.ToString());
                    cur.Clear();
                    cur.Append(ch);
                    addedFromText = 1;
                }
            }

            remainder = cur.ToString();
            return filled;
        }

        /// <summary>
        /// 将段落拆分为原子 token：
        ///   - "(N/M)"  → 一个整体
        ///   - 其他     → 按空格拆分
        /// </summary>
        private static List<string> Tokenize(string text)
        {
            var tokens = new List<string>();

            // 按 (N/M) 拆分，保留分隔符
            var parts = Regex.Split(text, @"(\(\d+/\d+\))");

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (Regex.IsMatch(part, @"^\(\d+/\d+\)$"))
                {
                    tokens.Add(part);
                }
                else
                {
                    foreach (var word in part.Split(' '))
                    {
                        var w = word.Trim();
                        if (w.Length > 0)
                            tokens.Add(w);
                    }
                }
            }

            return tokens;
        }
    }
}
