using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class HardeningActionHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => "Hardening";

        public HardeningActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Value; i++)
                _statusEffects.ApplyEffect(context.Source, StatusEffectType.Hardening, StatusEffectInstance.PermanentDuration, context.Source);
        }
    }
}
