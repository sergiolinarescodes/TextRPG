using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class PeckActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly ILuckService _luckService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Peck;

        public PeckActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
            _luckService = ctx.LuckService;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var offense = _entityStats.GetStat(context.Source, StatType.Strength);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var defense = _entityStats.GetStat(target, StatType.PhysicalDefense);
                var damage = StatScaling.OffensiveScale(context.Value, offense, defense);

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage, context.Source);
                _statusEffects?.ApplyEffect(target, StatusEffectType.Bleeding,
                    StatusEffectInstance.PermanentDuration, context.Source);
            }
        }
    }
}
