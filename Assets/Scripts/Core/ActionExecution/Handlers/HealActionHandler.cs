using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class HealActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => "Heal";

        public HealActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
                _entityStats.ApplyHeal(context.Targets[i], context.Value);
        }
    }
}
