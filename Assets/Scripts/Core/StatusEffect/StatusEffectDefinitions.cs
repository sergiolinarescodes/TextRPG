using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect
{
    public static class StatusEffectDefinitions
    {
        private static readonly Dictionary<StatusEffectType, StatusEffectDefinition> All = new()
        {
            [StatusEffectType.Burning] = new StatusEffectDefinition(
                StatusEffectType.Burning, "Burning", StackPolicy.RefreshDuration,
                DamagePerTick: 3, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false),

            [StatusEffectType.Wet] = new StatusEffectDefinition(
                StatusEffectType.Wet, "Wet", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false),

            [StatusEffectType.Poisoned] = new StatusEffectDefinition(
                StatusEffectType.Poisoned, "Poisoned", StackPolicy.StackIntensity,
                DamagePerTick: 2, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false),

            [StatusEffectType.Frozen] = new StatusEffectDefinition(
                StatusEffectType.Frozen, "Frozen", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.PhysicalDefense, 999),
                    new StatModifierEntry(StatType.MagicDefense, 999)
                }, GrantsExtraTurn: false),

            [StatusEffectType.Slowed] = new StatusEffectDefinition(
                StatusEffectType.Slowed, "Slowed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.MagicPower, -3) }, GrantsExtraTurn: false),

            [StatusEffectType.Cursed] = new StatusEffectDefinition(
                StatusEffectType.Cursed, "Cursed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Luck, -5) }, GrantsExtraTurn: false),

            [StatusEffectType.Buffed] = new StatusEffectDefinition(
                StatusEffectType.Buffed, "Buffed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Strength, 5) }, GrantsExtraTurn: false),

            [StatusEffectType.Shielded] = new StatusEffectDefinition(
                StatusEffectType.Shielded, "Shielded", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.PhysicalDefense, 5) }, GrantsExtraTurn: false),

            [StatusEffectType.ExtraTurn] = new StatusEffectDefinition(
                StatusEffectType.ExtraTurn, "Extra Turn", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: true),

            [StatusEffectType.Stun] = new StatusEffectDefinition(
                StatusEffectType.Stun, "Stunned", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false),

            [StatusEffectType.Concussion] = new StatusEffectDefinition(
                StatusEffectType.Concussion, "Concussion", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false),

            [StatusEffectType.Fear] = new StatusEffectDefinition(
                StatusEffectType.Fear, "Fear", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.Strength, -1),
                    new StatModifierEntry(StatType.MagicPower, -1),
                    new StatModifierEntry(StatType.PhysicalDefense, -1),
                    new StatModifierEntry(StatType.MagicDefense, -1)
                }, GrantsExtraTurn: false),
        };

        public static StatusEffectDefinition Get(StatusEffectType type) => All[type];
    }
}
