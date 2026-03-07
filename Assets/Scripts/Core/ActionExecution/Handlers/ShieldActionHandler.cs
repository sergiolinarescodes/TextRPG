using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ShieldActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => "Shield";

        public ShieldActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
                _entityStats.ApplyShield(context.Targets[i], context.Value);
        }
    }
}
