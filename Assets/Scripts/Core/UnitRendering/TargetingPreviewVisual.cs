using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using UnityEngine;

namespace TextRPG.Core.UnitRendering
{
    internal static class TargetingPreviewVisual
    {
        private static readonly Color HighlightEnemy = new(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HighlightSelf = new(0.3f, 1f, 0.3f, 0.4f);

        public static void ShowPreview(string text, CombatSlotVisual slotVisual,
            ICombatContext combatContext, ITargetingPreviewService previewService,
            ITargetingPreviewService ammoPreviewService, System.Func<string, bool> isAmmoWord)
        {
            ClearPreview(slotVisual);
            if (previewService == null) return;

            var word = text.ToLowerInvariant();

            bool isGive = WordPrefixHelper.TryStripGivePrefix(ref word);
            if (isGive)
                combatContext.SetTargetingInverted(true);
            else if (word.Length == 0) return;

            try
            {
                var svc = isAmmoWord(word) ? ammoPreviewService : previewService;
                var preview = svc.PreviewWord(word);
                if (preview.ActionPreviews.Count == 0) return;

                var sourceEntity = combatContext.SourceEntity;
                foreach (var actionPreview in preview.ActionPreviews)
                {
                    foreach (var entityId in actionPreview.AffectedEntities)
                    {
                        var element = slotVisual.GetSlotElement(entityId);
                        if (element == null) continue;
                        var color = entityId.Equals(sourceEntity) ? HighlightSelf : HighlightEnemy;
                        element.style.backgroundColor = color;
                    }
                }
            }
            finally
            {
                if (isGive) combatContext.SetTargetingInverted(false);
            }
        }

        public static void ClearPreview(CombatSlotVisual slotVisual)
        {
            var elements = slotVisual?.GetAllSlotElements();
            if (elements == null) return;
            for (int i = 0; i < elements.Count; i++)
                elements[i].style.backgroundColor = Color.black;
        }
    }
}
