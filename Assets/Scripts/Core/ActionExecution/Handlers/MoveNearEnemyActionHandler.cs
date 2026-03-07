using Unidad.Core.EventBus;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MoveNearEnemyActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;

        public string ActionId => "MoveNearEnemy";

        public MoveNearEnemyActionHandler(IActionHandlerContext ctx)
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
            var to = MovementHelper.StepToward(grid, context.Source, enemyPos, context.Value);

            if (!to.Equals(from))
                _eventBus.Publish(new MovementActionEvent(context.Source, from, to, "MoveNearEnemy", false));
        }
    }
}
