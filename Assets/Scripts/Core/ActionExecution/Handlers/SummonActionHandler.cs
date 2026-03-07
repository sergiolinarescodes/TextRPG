using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.Grid;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class SummonActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEntityStatsService _entityStats;
        private int _summonCounter;

        public string ActionId => "Summon";

        public SummonActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;
            if (grid == null) return;

            var sourcePos = grid.GetPosition(context.Source);
            var spawnPos = FindAdjacentEmpty(grid, sourcePos);
            if (spawnPos == null) return;

            var id = $"summon_{_summonCounter++}";
            var entityId = new EntityId(id);
            var hp = context.Value * 5;

            _entityStats.RegisterEntity(entityId, maxHealth: hp, strength: context.Value,
                magicPower: 0, physicalDefense: context.Value / 2, magicDefense: 0, luck: 0);

            var unitDef = new UnitDefinition(new UnitId(id), "SUMMON", hp,
                context.Value, 0, 0, new Color(0.6f, 0.3f, 1f));
            grid.RegisterCombatant(entityId, unitDef, spawnPos.Value);
        }

        private static GridPosition? FindAdjacentEmpty(CombatGrid.ICombatGridService grid, GridPosition center)
        {
            foreach (var neighbor in grid.Grid.GetNeighbors(center, NeighborMode.Cardinal))
            {
                if (grid.CanMoveTo(neighbor))
                    return neighbor;
            }
            return null;
        }
    }
}
