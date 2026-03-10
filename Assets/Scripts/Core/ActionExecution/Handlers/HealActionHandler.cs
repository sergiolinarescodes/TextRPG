using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class HealActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Heal;

        public HealActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            var sourceMagic = _entityStats.GetStat(context.Source, StatType.MagicPower);
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var healAmount = StatScaling.SupportScale(context.Value, sourceMagic);
                if (_statusEffects != null && _statusEffects.HasEffect(context.Targets[i], StatusEffectType.Poisoned))
                    healAmount = Math.Max(1, healAmount / 2);
                _entityStats.ApplyHeal(context.Targets[i], healAmount);
            }
        }
    }
}
