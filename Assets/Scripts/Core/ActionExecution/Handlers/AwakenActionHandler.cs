using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class AwakenActionHandler : IActionHandler
    {
        private static readonly StatusEffectType[] CrowdControlStatuses =
        {
            StatusEffectType.Sleep,
            StatusEffectType.Stun,
            StatusEffectType.Frostbitten,
        };

        private readonly IStatusEffectService _statusEffects;
        private readonly IEntityStatsService _entityStats;

        public string ActionId => ActionNames.Awaken;

        public AwakenActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];

                for (int s = 0; s < CrowdControlStatuses.Length; s++)
                {
                    if (_statusEffects.HasEffect(target, CrowdControlStatuses[s]))
                        _statusEffects.RemoveEffect(target, CrowdControlStatuses[s]);
                }

                _statusEffects.ApplyEffect(target, StatusEffectType.Awakened, 1, context.Source);
            }
        }
    }
}
