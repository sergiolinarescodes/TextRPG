using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ConcentrateActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Concentrate;

        public ConcentrateActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            var sourceMagic = _entityStats.GetStat(context.Source, StatType.MagicPower);
            var manaGain = StatScaling.SupportScale(context.Value, sourceMagic, StatScaling.WeakDivisor);
            _entityStats.ApplyMana(context.Source, manaGain);
            _statusEffects.ApplyEffect(context.Source, StatusEffectType.Concentrated, 2, context.Source);
        }
    }
}
