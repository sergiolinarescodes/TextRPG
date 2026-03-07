using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public interface IStatusEffectService
    {
        void ApplyEffect(EntityId target, StatusEffectType type, int duration, EntityId source);
        void RemoveEffect(EntityId target, StatusEffectType type);
        void RemoveAllEffects(EntityId target);
        bool HasEffect(EntityId target, StatusEffectType type);
        IReadOnlyList<StatusEffectInstance> GetEffects(EntityId target);
        int GetStackCount(EntityId target, StatusEffectType type);
    }
}
