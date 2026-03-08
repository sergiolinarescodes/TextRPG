using UnityEngine;

namespace TextRPG.Core.StatusEffect
{
    public sealed record StatusEffectDefinition(
        StatusEffectType Type,
        string DisplayName,
        StackPolicy StackPolicy,
        int? DamagePerTick,
        StatModifierEntry[] StatModifiers,
        bool GrantsExtraTurn,
        Color DisplayColor = default,
        string Description = ""
    );
}
