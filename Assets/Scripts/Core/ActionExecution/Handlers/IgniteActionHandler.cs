using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class IgniteActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Ignite;

        public IgniteActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            var offense = _entityStats.GetStat(context.Source, StatType.MagicPower);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var defense = _entityStats.GetStat(target, StatType.MagicDefense);
                var damage = StatScaling.OffensiveScale(context.Value, offense, defense);
                _entityStats.ApplyDamage(target, damage, context.Source);
                _statusEffects?.ApplyEffect(target, StatusEffectType.Burning,
                    context.Value, context.Source);
            }
        }
    }
}
