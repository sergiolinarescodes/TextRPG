using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;


namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class ExtraTurnHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.ExtraTurn;

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            ctx.TurnService.GrantExtraTurn(target);
        }
    }
}
