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
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var magic = _entityStats.GetStat(target, StatType.MagicPower);
                var manaGain = StatScaling.SupportScale(context.Value, magic, StatScaling.WeakDivisor);
                _entityStats.ApplyMana(target, manaGain);
                _statusEffects.ApplyEffect(target, StatusEffectType.Concentrated, 2, context.Source);
            }
        }
    }
}
