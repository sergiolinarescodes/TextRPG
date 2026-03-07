using System.Collections.Generic;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.Grid;

namespace TextRPG.Core.ActionExecution
{
    public interface ICombatContext
    {
        EntityId SourceEntity { get; }
        EntityId? FocusedTarget { get; }
        GridPosition? FocusedPosition { get; }
        ICombatGridService CombatGrid { get; }
        IReadOnlyList<EntityId> GetTargets(TargetType targetType, int range = 0);
        IReadOnlyList<EntityId> ExpandArea(IReadOnlyList<EntityId> primaryTargets, AreaShape area);
        void SetSourceEntity(EntityId source);
        void SetEnemies(IReadOnlyList<EntityId> enemies);
        void SetAllies(IReadOnlyList<EntityId> allies);
        void SetGrid(ICombatGridService grid);
        void SetEntityStats(IEntityStatsService entityStats);
        void SetStatusEffects(IStatusEffectService statusEffects);
        void SetFocusedTarget(EntityId target);
        void ClearFocusedTarget();
        void SetFocusedPosition(GridPosition position);
        void ClearFocusedPosition();
    }
}
