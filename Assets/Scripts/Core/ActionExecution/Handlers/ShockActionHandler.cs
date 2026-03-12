using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ShockActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly ICombatContext _combatContext;
        private readonly StatusEffectInteractionTable _interactionTable;
        private readonly ILuckService _luckService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Shock;

        public ShockActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
            _combatContext = ctx.CombatContext;
            _interactionTable = ctx.InteractionTable;
            _luckService = ctx.LuckService;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var sourceMagic = _entityStats.GetStat(context.Source, StatType.MagicPower);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var targetMDef = _entityStats.GetStat(target, StatType.MagicDefense);
                var multiplier = _interactionTable.GetDamageMultiplier(ActionNames.Shock, target, _statusEffects);
                var damage = Math.Max(1, (int)(StatScaling.SupportScale(context.Value, sourceMagic) * multiplier) - targetMDef / 3);

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage);
            }
        }
    }
}
