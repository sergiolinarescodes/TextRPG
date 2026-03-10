using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.WordAction;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class KeywordSubTooltipProvider
    {
        private readonly IActionRegistry _actionRegistry;
        private readonly IActionHandlerRegistry _handlerRegistry;

        public KeywordSubTooltipProvider(IActionRegistry actionRegistry, IActionHandlerRegistry handlerRegistry)
        {
            _actionRegistry = actionRegistry;
            _handlerRegistry = handlerRegistry;
        }

        public IReadOnlyList<SubTooltipEntry> DetectKeywords(IReadOnlyList<WordActionMapping> actions)
        {
            if (actions == null || actions.Count == 0)
                return System.Array.Empty<SubTooltipEntry>();

            var seen = new HashSet<string>();
            var keywords = new List<(string Id, string Desc, Color Color)>();

            foreach (var action in actions)
            {
                var id = action.ActionId;
                if (id == "Item" || id == "Weapon") continue;
                if (!seen.Add(id)) continue;
                if (_handlerRegistry != null && !_handlerRegistry.TryGet(id, out _)) continue;
                if (!ActionDescriptionProvider.Has(id)) continue;

                keywords.Add((id, ActionDescriptionProvider.Get(id),
                    ActionDescriptions.GetColor(id, _actionRegistry)));
            }

            if (keywords.Count == 0)
                return System.Array.Empty<SubTooltipEntry>();

            // Single sub-tooltip with all keywords as a list
            var content = TooltipContent.FromCustom(() =>
            {
                var container = new VisualElement();
                container.pickingMode = PickingMode.Ignore;

                for (int i = 0; i < keywords.Count; i++)
                {
                    var (id, desc, color) = keywords[i];

                    if (i > 0)
                    {
                        var spacer = new VisualElement();
                        spacer.style.height = 6;
                        spacer.pickingMode = PickingMode.Ignore;
                        container.Add(spacer);
                    }

                    var nameLabel = new Label(id);
                    nameLabel.style.color = color;
                    nameLabel.style.fontSize = 24;
                    nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    nameLabel.style.marginBottom = 2;
                    nameLabel.pickingMode = PickingMode.Ignore;
                    container.Add(nameLabel);

                    var descLabel = new Label(desc);
                    descLabel.style.color = Color.white;
                    descLabel.style.fontSize = 22;
                    descLabel.style.whiteSpace = WhiteSpace.Normal;
                    descLabel.pickingMode = PickingMode.Ignore;
                    container.Add(descLabel);
                }

                return container;
            });

            return new[] { new SubTooltipEntry(content, TooltipStyles.SubKeywordTooltip) };
        }
    }
}
