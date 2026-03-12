using System.Linq;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class CombustActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly ILuckService _luckService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Combust;

        public CombustActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
            _luckService = ctx.LuckService;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var offense = _entityStats.GetStat(context.Source, StatType.MagicPower);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var defense = _entityStats.GetStat(target, StatType.MagicDefense);

                int baseValue = context.Value;
                if (_statusEffects != null && _statusEffects.HasEffect(target, StatusEffectType.Burning))
                {
                    var burning = _statusEffects.GetEffects(target)
                        .FirstOrDefault(e => e.Type == StatusEffectType.Burning);
                    if (burning != null)
                        baseValue += burning.RemainingDuration;
                    _statusEffects.RemoveEffect(target, StatusEffectType.Burning);
                }

                var damage = StatScaling.OffensiveScale(baseValue, offense, defense);

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage, context.Source);
            }
        }
    }
}
