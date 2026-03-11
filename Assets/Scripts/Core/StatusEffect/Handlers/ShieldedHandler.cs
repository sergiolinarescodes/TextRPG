using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class ShieldedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Shielded;
    }
}
