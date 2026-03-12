using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class DeceiveActionHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => ActionNames.Deceive;

        public DeceiveActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                _statusEffects.ApplyEffect(target, StatusEffectType.Fear, context.Value, context.Source);
                _statusEffects.ApplyEffect(target, StatusEffectType.Concussion,
                    StatusEffectInstance.PermanentDuration, context.Source);
            }
        }
    }
}
