using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ConcentrateActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => "Concentrate";

        public ConcentrateActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            _entityStats.ApplyMana(context.Source, context.Value);
            _statusEffects.ApplyEffect(context.Source, StatusEffectType.Concentrated, 2, context.Source);
        }
    }
}
