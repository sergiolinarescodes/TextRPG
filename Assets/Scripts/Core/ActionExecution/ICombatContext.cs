using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution
{
    public interface ICombatContext
    {
        EntityId SourceEntity { get; }
        EntityId? FocusedTarget { get; }
        ICombatSlotService SlotService { get; }
        IReadOnlyList<EntityId> GetTargets(TargetType targetType, int range = 0, StatusEffectType? statusFilter = null);
        void SetSourceEntity(EntityId source);
        void SetEnemies(IReadOnlyList<EntityId> enemies);
        void SetAllies(IReadOnlyList<EntityId> allies);
        void SetSlotService(ICombatSlotService slotService);
        void SetEntityStats(IEntityStatsService entityStats);
        void SetStatusEffects(IStatusEffectService statusEffects);
        void SetFocusedTarget(EntityId target);
        void ClearFocusedTarget();
        void SetPassiveService(IPassiveService passiveService);

        void SetTargetingInverted(bool inverted);

        bool IsGiveCommand { get; }
        void SetGiveCommand(bool isGive);
    }
}
