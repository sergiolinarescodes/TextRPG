using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MoveFlankActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;

        public string ActionId => "MoveFlank";

        public MoveFlankActionHandler(IActionHandlerContext ctx)
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
            var behind = MovementHelper.FindBehindPosition(from, enemyPos);

            if (grid.Grid.IsInBounds(behind) && grid.CanMoveTo(behind)
                && from.ManhattanDistanceTo(behind) <= context.Value)
            {
                grid.MoveEntity(context.Source, behind);
                _eventBus.Publish(new MovementActionEvent(context.Source, from, behind, "MoveFlank", true));
                return;
            }

            // Fallback: try perpendicular tiles adjacent to enemy
            var dx = enemyPos.X - from.X;
            var dy = enemyPos.Y - from.Y;
            GridPosition[] perpendiculars = dx == 0 || dy == 0
                ? new[]
                {
                    new GridPosition(enemyPos.X + (dy == 0 ? 0 : 1), enemyPos.Y + (dx == 0 ? 0 : 1)),
                    new GridPosition(enemyPos.X - (dy == 0 ? 0 : 1), enemyPos.Y - (dx == 0 ? 0 : 1))
                }
                : new[]
                {
                    new GridPosition(enemyPos.X, enemyPos.Y + (dy > 0 ? 1 : -1)),
                    new GridPosition(enemyPos.X + (dx > 0 ? 1 : -1), enemyPos.Y)
                };

            foreach (var pos in perpendiculars)
            {
                if (grid.Grid.IsInBounds(pos) && grid.CanMoveTo(pos)
                    && from.ManhattanDistanceTo(pos) <= context.Value)
                {
                    grid.MoveEntity(context.Source, pos);
                    _eventBus.Publish(new MovementActionEvent(context.Source, from, pos, "MoveFlank", true));
                    return;
                }
            }
        }
    }
}
