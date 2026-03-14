using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class CauterizeActionHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Cauterize;

        public CauterizeActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                if (_statusEffects.HasEffect(target, StatusEffectType.Bleeding))
                    _statusEffects.RemoveEffect(target, StatusEffectType.Bleeding);
            }
        }
    }
}
