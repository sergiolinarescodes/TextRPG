using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class RecuperateActionHandler : IActionHandler
    {
        private static readonly StatusEffectType[] NegativeStatuses =
        {
            StatusEffectType.Stun,
            StatusEffectType.Fear,
            StatusEffectType.Sleep,
            StatusEffectType.Frostbitten,
            StatusEffectType.Burning,
            StatusEffectType.Poisoned,
            StatusEffectType.Bleeding,
            StatusEffectType.Concussion,
            StatusEffectType.Cursed,
            StatusEffectType.Slowed,
            StatusEffectType.Drunk,
            StatusEffectType.Tired,
            StatusEffectType.Anxiety,
            StatusEffectType.Frozen,
        };

        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Recuperate;

        public RecuperateActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            var sourceMagic = _entityStats.GetStat(context.Source, StatType.MagicPower);
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];

                // Heal
                var healAmount = StatScaling.SupportScale(context.Value, sourceMagic);
                _entityStats.ApplyHeal(target, healAmount);

                // Remove one random negative status
                RemoveOneNegativeStatus(target);
            }
        }

        private void RemoveOneNegativeStatus(EntityId target)
        {
            // Collect active negative statuses
            Span<StatusEffectType> active = stackalloc StatusEffectType[NegativeStatuses.Length];
            int count = 0;
            for (int i = 0; i < NegativeStatuses.Length; i++)
            {
                if (_statusEffects.HasEffect(target, NegativeStatuses[i]))
                    active[count++] = NegativeStatuses[i];
            }

            if (count == 0) return;

            // Pick one at random
            var pick = active[UnityEngine.Random.Range(0, count)];
            _statusEffects.RemoveEffect(target, pick);
        }
    }
}
