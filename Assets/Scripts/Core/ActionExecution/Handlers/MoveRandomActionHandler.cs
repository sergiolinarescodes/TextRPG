using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MoveRandomActionHandler : IActionHandler
    {
        private readonly ICombatContext _combatContext;
        private readonly IEventBus _eventBus;
        private readonly System.Random _rng = new();

        public string ActionId => "MoveRandom";

        public MoveRandomActionHandler(IActionHandlerContext ctx)
        {
            _combatContext = ctx.CombatContext;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;
            if (grid == null) return;

            var from = grid.GetPosition(context.Source);
            var candidates = MovementHelper.CollectEmptyInRange(grid, from, context.Value);
            if (candidates.Count == 0) return;

            var dest = candidates[_rng.Next(candidates.Count)];
            grid.MoveEntity(context.Source, dest);
            _eventBus.Publish(new MovementActionEvent(context.Source, from, dest, "MoveRandom", true));
        }
    }
}
