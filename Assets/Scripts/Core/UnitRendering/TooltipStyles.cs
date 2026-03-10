using Unidad.Core.UI.Tooltip;
using UnityEngine;

namespace TextRPG.Core.UnitRendering
{
    internal static class TooltipStyles
    {
        public static readonly TooltipStyle EntityTooltip = new()
        {
            ShowArrow = false,
            FontSize = 26,
            MaxWidth = 350,
            BackgroundColor = Color.black,
            BorderColor = Color.white,
            BorderWidth = 1,
            BorderRadius = 0,
            PaddingH = 12,
            PaddingV = 10,
            SubTooltipDelayMs = 1000f
        };

        public static readonly TooltipStyle SubKeywordTooltip = new()
        {
            ShowArrow = false,
            FontSize = 26,
            MaxWidth = 350,
            BackgroundColor = Color.black,
            BorderColor = Color.white,
            BorderWidth = 1,
            BorderRadius = 0,
            PaddingH = 12,
            PaddingV = 10
        };
    }
}
