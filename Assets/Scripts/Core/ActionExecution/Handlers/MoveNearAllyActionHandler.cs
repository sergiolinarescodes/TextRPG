using Unidad.Core.EventBus;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MoveNearAllyActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;

        public string ActionId => "MoveNearAlly";

        public MoveNearAllyActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;
            if (grid == null) return;
            if (context.Targets.Count == 0) return;

            var from = grid.GetPosition(context.Source);
            var nearestAlly = MovementHelper.FindNearestEntity(grid, from, context.Targets);
            if (nearestAlly == null) return;

            var allyPos = grid.GetPosition(nearestAlly.Value);
            var to = MovementHelper.StepToward(grid, context.Source, allyPos, context.Value);

            if (!to.Equals(from))
                _eventBus.Publish(new MovementActionEvent(context.Source, from, to, "MoveNearAlly", false));
        }
    }
}
