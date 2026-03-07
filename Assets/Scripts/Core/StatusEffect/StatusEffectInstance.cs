using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public sealed class StatusEffectInstance
    {
        public StatusEffectType Type { get; }
        public int RemainingDuration { get; internal set; }
        public EntityId Source { get; }
        public string[] ModifierIds { get; }
        public int StackCount { get; internal set; } = 1;

        public StatusEffectInstance(StatusEffectType type, int duration, EntityId source, string[] modifierIds)
        {
            Type = type;
            RemainingDuration = duration;
            Source = source;
            ModifierIds = modifierIds;
        }
    }
}
