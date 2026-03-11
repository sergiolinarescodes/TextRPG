using System.Collections.Generic;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class PurifyActionHandler : IActionHandler
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

        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Purify;

        public PurifyActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                int removed = 0;
                for (int s = 0; s < NegativeStatuses.Length && removed < context.Value; s++)
                {
                    if (_statusEffects.HasEffect(target, NegativeStatuses[s]))
                    {
                        _statusEffects.RemoveEffect(target, NegativeStatuses[s]);
                        removed++;
                    }
                }
            }
        }
    }
}
