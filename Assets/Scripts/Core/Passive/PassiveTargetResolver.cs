using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveTargetResolver
    {
        public IReadOnlyList<EntityId> Resolve(string target, PassiveTriggerContext triggerCtx,
                                                EntityId owner, IPassiveContext ctx)
        {
            return target switch
            {
                "Self" => new[] { owner },
                "Injured" => ResolveContextual(triggerCtx.EventEntity),
                "Attacker" => ResolveContextual(triggerCtx.EventSource),
                "AllAllies" => ResolveAllAllies(owner, ctx),
                "AllEnemies" => ResolveAllEnemies(owner, ctx),
                _ => new[] { owner }
            };
        }

        private static EntityId[] ResolveContextual(EntityId? entity)
        {
            return entity.HasValue ? new[] { entity.Value } : Array.Empty<EntityId>();
        }

        private static EntityId[] ResolveAllAllies(EntityId owner, IPassiveContext ctx)
        {
            var ownerSlot = ctx.SlotService.GetSlot(owner);
            bool isAllySide = !ownerSlot.HasValue || ownerSlot.Value.Type == SlotType.Ally;

            var slotAllies = isAllySide ? ctx.SlotService.GetAllAllies() : ctx.SlotService.GetAllEnemies();
            var result = new List<EntityId>();

            for (int i = 0; i < slotAllies.Count; i++)
            {
                if (ctx.EntityStats.HasEntity(slotAllies[i]) && ctx.EntityStats.GetCurrentHealth(slotAllies[i]) > 0)
                    result.Add(slotAllies[i]);
            }

            // Include the player when owner is ally-faction
            if (isAllySide && ctx.EncounterService != null)
            {
                var player = ctx.EncounterService.PlayerEntity;
                if (ctx.EntityStats.HasEntity(player) && ctx.EntityStats.GetCurrentHealth(player) > 0)
                    result.Add(player);
            }

            return result.ToArray();
        }

        private static EntityId[] ResolveAllEnemies(EntityId owner, IPassiveContext ctx)
        {
            var ownerSlot = ctx.SlotService.GetSlot(owner);
            bool isAllySide = !ownerSlot.HasValue || ownerSlot.Value.Type == SlotType.Ally;

            var enemies = isAllySide ? ctx.SlotService.GetAllEnemies() : ctx.SlotService.GetAllAllies();
            var result = new List<EntityId>();

            for (int i = 0; i < enemies.Count; i++)
            {
                if (ctx.EntityStats.HasEntity(enemies[i]) && ctx.EntityStats.GetCurrentHealth(enemies[i]) > 0)
                    result.Add(enemies[i]);
            }

            return result.ToArray();
        }

        internal static bool IsSameFaction(EntityId a, EntityId b, IPassiveContext ctx)
        {
            var slotA = ctx.SlotService.GetSlot(a);
            var slotB = ctx.SlotService.GetSlot(b);

            if (slotA.HasValue && slotB.HasValue)
                return slotA.Value.Type == slotB.Value.Type;

            if (ctx.EncounterService == null) return false;

            var player = ctx.EncounterService.PlayerEntity;
            bool aIsPlayerSide = a.Equals(player) || (slotA.HasValue && slotA.Value.Type == SlotType.Ally);
            bool bIsPlayerSide = b.Equals(player) || (slotB.HasValue && slotB.Value.Type == SlotType.Ally);
            return aIsPlayerSide == bIsPlayerSide;
        }
    }
}
