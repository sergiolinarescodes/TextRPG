using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class SunderActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Sunder;

        public SunderActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var effects = _statusEffects.GetEffects(target);
                int removed = 0;

                for (int e = effects.Count - 1; e >= 0 && removed < context.Value; e--)
                {
                    var def = StatusEffectDefinitions.Get(effects[e].Type);
                    if (def.IsPositive)
                    {
                        _statusEffects.RemoveEffect(target, effects[e].Type);
                        removed++;
                    }
                }

                if (removed > 0)
                    _entityStats.ApplyDamage(target, removed, context.Source);
            }
        }
    }
}
