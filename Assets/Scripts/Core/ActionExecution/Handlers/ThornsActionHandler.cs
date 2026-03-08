using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ThornsActionHandler : IActionHandler
    {
        private readonly IStatusEffectService _statusEffects;

        public string ActionId => "Thorns";

        public ThornsActionHandler(IActionHandlerContext ctx)
        {
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            _statusEffects.ApplyEffect(context.Source, StatusEffectType.Thorns, context.Value, context.Source);
        }
    }
}
