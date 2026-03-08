using System;
using TextAnimationsForUIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    public static class UnitTextLayout
    {
        private const float CharWidthRatio = 0.6f;
        private const float LineHeightRatio = 1.2f;

        public static int BestCharsPerRow(int length)
        {
            if (length <= 0) return 1;
            if (length <= 2) return length;

            int best = length;
            float bestRatio = float.MaxValue;

            int sqrt = Mathf.RoundToInt(Mathf.Sqrt(length));
            int lo = Mathf.Max(1, sqrt - 1);
            int hi = Mathf.Min(length, sqrt + 1);

            for (int c = lo; c <= hi; c++)
            {
                int rows = (int)Math.Ceiling((double)length / c);
                float mx = Mathf.Max(c, rows);
                float mn = Mathf.Min(c, rows);
                float ratio = mx / mn;

                if (ratio < bestRatio || (Mathf.Approximately(ratio, bestRatio) && c > best))
                {
                    bestRatio = ratio;
                    best = c;
                }
            }

            return best;
        }

        public static TextLayoutResult Calculate(string name, float tileWidth, float tileHeight)
        {
            if (string.IsNullOrEmpty(name))
                return new TextLayoutResult(Array.Empty<string>(), 0, 0, 12f);

            int charsPerRow = BestCharsPerRow(name.Length);
            int rowCount = (int)Math.Ceiling((double)name.Length / charsPerRow);

            var rows = new string[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                int start = i * charsPerRow;
                int len = Mathf.Min(charsPerRow, name.Length - start);
                rows[i] = name.Substring(start, len);
            }

            float fontByWidth = tileWidth / (charsPerRow * CharWidthRatio);
            float fontByHeight = tileHeight / (rowCount * LineHeightRatio);
            float fontSize = Mathf.Max(4f, Mathf.Min(fontByWidth, fontByHeight));

            return new TextLayoutResult(rows, charsPerRow, rowCount, fontSize);
        }
    }

    public readonly record struct TextLayoutResult(string[] Rows, int CharsPerRow, int RowCount, float FontSize);

    public static class UnitTextLabels
    {
        public static Label[] AddTo(TextLayoutResult layout, Color textColor, VisualElement container)
        {
            var labels = new Label[layout.Rows.Length];
            for (int i = 0; i < layout.Rows.Length; i++)
            {
                var label = new Label(layout.Rows[i]);
                label.style.fontSize = layout.FontSize;
                label.style.color = textColor;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 0;
                label.style.marginBottom = 0;
                label.style.paddingTop = 0;
                label.style.paddingBottom = 0;
                container.Add(label);
                labels[i] = label;
            }
            return labels;
        }

        public static AnimatedLabel[] AddAnimatedWithRecipeTo(TextLayoutResult layout, Color textColor,
            VisualElement container, string markupTemplate)
        {
            var labels = new AnimatedLabel[layout.Rows.Length];
            for (int i = 0; i < layout.Rows.Length; i++)
            {
                var label = new AnimatedLabel();
                label.style.fontSize = layout.FontSize;
                label.style.color = textColor;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 0;
                label.style.marginBottom = 0;
                label.style.paddingTop = 0;
                label.style.paddingBottom = 0;
                label.text = string.Format(markupTemplate, layout.Rows[i]);
                label.Play();
                container.Add(label);
                labels[i] = label;
            }
            return labels;
        }
    }
}
