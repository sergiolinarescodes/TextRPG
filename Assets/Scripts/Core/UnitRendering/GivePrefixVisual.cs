using System.Collections.Generic;
using UnityEngine;
using Unidad.Core.UI.Components;

namespace TextRPG.Core.UnitRendering
{
    internal static class GivePrefixVisual
    {
        public static readonly Color GivePrefixColor = new(0.55f, 0.65f, 0.82f);
        private static readonly List<int> GivePrefixIndices = new() { 0, 1, 2, 3, 4 };

        /// <summary>
        /// Detects "give " prefix in display text, applies color to prefix labels,
        /// and triggers wave animation on first detection.
        /// Returns (isGivePrefix, matchWord, prefixLen).
        /// </summary>
        public static (bool IsGive, string MatchWord, int PrefixLen) ApplyGivePrefix(
            string displayText, AnimatedCodeField codeField, ref bool givePrefixDetected)
        {
            var lowerText = displayText.ToLowerInvariant();
            bool isGivePrefix = lowerText.StartsWith("give ");
            var matchWord = isGivePrefix ? lowerText.Substring(5) : displayText;
            int prefixLen = isGivePrefix ? 5 : 0;

            // Wave animation on "give " when first detected
            if (isGivePrefix && !givePrefixDetected)
            {
                givePrefixDetected = true;
                codeField.PlayHighlightAnimation(GivePrefixIndices);
            }
            else if (!isGivePrefix)
            {
                givePrefixDetected = false;
            }

            // Apply "give " prefix color
            if (isGivePrefix)
            {
                var labels = codeField.CharLabels;
                for (int i = 0; i < prefixLen && i < labels.Count; i++)
                    labels[i].style.color = GivePrefixColor;
            }

            return (isGivePrefix, matchWord, prefixLen);
        }
    }
}
