using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class AnxietyHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Anxiety;
    }
}
