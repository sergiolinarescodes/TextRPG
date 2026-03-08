using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public sealed class StatusEffectInstance
    {
        public const int PermanentDuration = -1;

        public StatusEffectType Type { get; }
        public int RemainingDuration { get; internal set; }
        public bool IsPermanent => RemainingDuration < 0;
        public EntityId Source { get; }
        public string[] ModifierIds { get; }
        public int StackCount { get; internal set; } = 1;
        public bool WasHealedThisTurn { get; internal set; }

        public StatusEffectInstance(StatusEffectType type, int duration, EntityId source, string[] modifierIds)
        {
            Type = type;
            RemainingDuration = duration;
            Source = source;
            ModifierIds = modifierIds;
        }
    }
}
