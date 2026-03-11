using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;

namespace TextRPG.Core.EventEncounter.Reactions.Tags
{
    internal static class TagRecruitmentHelper
    {
        public static bool TryRecruit(TagReactionContext ctx)
        {
            var emptySlot = ctx.SlotService.FindFirstEmptySlot(SlotType.Ally);
            if (emptySlot == null) return false;

            // Get unit name for ability lookup (e.g., "sellsword" → matches unit registry key)
            string unitWord = "recruited";
            try
            {
                var def = ctx.EncounterService?.GetDefinition(ctx.Target);
                if (def != null) unitWord = def.Name.ToLowerInvariant();
            }
            catch { /* fallback to "recruited" */ }

            ctx.SlotService.RemoveEntity(ctx.Target);
            ctx.SlotService.RegisterAlly(ctx.Target, emptySlot.Value);

            var slot = new CombatSlot.CombatSlot(SlotType.Ally, emptySlot.Value);
            ctx.EventBus.Publish(new EntityRecruitedEvent(ctx.Target, ctx.Source));
            ctx.EventBus.Publish(new UnitSummonedEvent(ctx.Target, ctx.Source, slot, "ally", unitWord));
            return true;
        }
    }
}
