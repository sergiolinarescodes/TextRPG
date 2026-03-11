using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class StunHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Stun;
    }
}
