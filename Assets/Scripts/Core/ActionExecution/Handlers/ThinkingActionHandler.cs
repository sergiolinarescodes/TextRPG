using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ThinkingActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => ActionNames.Thinking;

        public ThinkingActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
                _entityStats.ApplyMana(context.Targets[i], context.Value);
        }
    }
}
