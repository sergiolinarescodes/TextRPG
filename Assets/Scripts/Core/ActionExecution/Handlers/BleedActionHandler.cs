using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class BleedActionHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => "Bleed";

        public BleedActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
                _statusEffects.ApplyEffect(context.Targets[i], StatusEffectType.Bleeding, StatusEffectInstance.PermanentDuration, context.Source);
        }
    }
}
