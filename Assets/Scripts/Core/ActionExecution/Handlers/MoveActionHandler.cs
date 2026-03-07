using Unidad.Core.EventBus;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MoveActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;

        public string ActionId => "Move";

        public MoveActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;
            if (grid == null) return;

            var from = grid.GetPosition(context.Source);
            var focusedPos = _combatContext.FocusedPosition;

            if (focusedPos.HasValue)
            {
                var dist = from.ManhattanDistanceTo(focusedPos.Value);
                if (dist <= context.Value && grid.CanMoveTo(focusedPos.Value))
                {
                    grid.MoveEntity(context.Source, focusedPos.Value);
                    _eventBus.Publish(new MovementActionEvent(context.Source, from, focusedPos.Value, "Move", false));
                }
                return;
            }

            EntityId? nearestEnemy = null;
            if (context.Targets.Count > 0)
                nearestEnemy = MovementHelper.FindNearestEntity(grid, from, context.Targets);

            if (nearestEnemy == null) return;

            var targetPos = grid.GetPosition(nearestEnemy.Value);
            var to2 = MovementHelper.StepToward(grid, context.Source, targetPos, context.Value);

            if (!to2.Equals(from))
                _eventBus.Publish(new MovementActionEvent(context.Source, from, to2, "Move", false));
        }
    }
}
