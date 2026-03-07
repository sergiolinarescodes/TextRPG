using Unidad.Core.EventBus;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class TeleportActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;

        public string ActionId => "Teleport";

        public TeleportActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;
            if (grid == null) return;

            var from = grid.GetPosition(context.Source);

            EntityId? nearestEnemy = null;
            if (context.Targets.Count > 0)
                nearestEnemy = MovementHelper.FindNearestEntity(grid, from, context.Targets);

            if (nearestEnemy == null) return;

            var enemyPos = grid.GetPosition(nearestEnemy.Value);
            var adjacent = MovementHelper.FindEmptyAdjacentTo(grid, enemyPos);

            Unidad.Core.Grid.GridPosition? best = null;
            var bestDist = int.MaxValue;

            foreach (var pos in adjacent)
            {
                var dist = from.ManhattanDistanceTo(pos);
                if (dist <= context.Value && dist < bestDist)
                {
                    bestDist = dist;
                    best = pos;
                }
            }

            if (best == null) return;

            grid.MoveEntity(context.Source, best.Value);
            _eventBus.Publish(new MovementActionEvent(context.Source, from, best.Value, "Teleport", true));
        }
    }
}
