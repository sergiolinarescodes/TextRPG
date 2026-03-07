using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class ExtraTurnHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.ExtraTurn;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            ctx.TurnService.GrantExtraTurn(target);
        }
    }
}
