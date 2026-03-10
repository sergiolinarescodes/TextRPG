using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using UnityEngine;

namespace TextRPG.Core.StatusEffect
{
    public static class StatusEffectDefinitions
    {
        private static readonly Dictionary<StatusEffectType, StatusEffectDefinition> All = new()
        {
            [StatusEffectType.Burning] = new StatusEffectDefinition(
                StatusEffectType.Burning, "Burning", StackPolicy.RefreshDuration,
                DamagePerTick: 3, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.6f, 0f), Description: "Takes 3 fire damage per turn"),

            [StatusEffectType.Wet] = new StatusEffectDefinition(
                StatusEffectType.Wet, "Wet", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.3f, 0.7f, 1f), Description: "Soaked, vulnerable to shock"),

            [StatusEffectType.Poisoned] = new StatusEffectDefinition(
                StatusEffectType.Poisoned, "Poisoned", StackPolicy.StackIntensity,
                DamagePerTick: 2, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0.1f, 0.9f), Description: "Takes damage per stack each turn"),

            [StatusEffectType.Frozen] = new StatusEffectDefinition(
                StatusEffectType.Frozen, "Frozen", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.PhysicalDefense, 999),
                    new StatModifierEntry(StatType.MagicDefense, 999)
                }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.9f, 1f), Description: "Encased in ice, cannot act"),

            [StatusEffectType.Slowed] = new StatusEffectDefinition(
                StatusEffectType.Slowed, "Slowed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.MagicPower, -3) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0.6f, 0.8f), Description: "Magic power reduced"),

            [StatusEffectType.Cursed] = new StatusEffectDefinition(
                StatusEffectType.Cursed, "Cursed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Luck, -5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0f, 0.5f), Description: "Luck reduced"),

            [StatusEffectType.Buffed] = new StatusEffectDefinition(
                StatusEffectType.Buffed, "Buffed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Strength, 5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.2f, 1f, 0.3f), Description: "Strength increased"),

            [StatusEffectType.Shielded] = new StatusEffectDefinition(
                StatusEffectType.Shielded, "Shielded", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.PhysicalDefense, 5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0.6f, 0.6f), Description: "Physical defense increased"),

            [StatusEffectType.ExtraTurn] = new StatusEffectDefinition(
                StatusEffectType.ExtraTurn, "Extra Turn", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: true,
                DisplayColor: new Color(1f, 0.9f, 0.4f), Description: "Gains an extra turn"),

            [StatusEffectType.Stun] = new StatusEffectDefinition(
                StatusEffectType.Stun, "Stunned", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 1f, 0.2f), Description: "Cannot act this turn"),

            [StatusEffectType.Concussion] = new StatusEffectDefinition(
                StatusEffectType.Concussion, "Concussion", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.7f, 0.3f), Description: "Increasing confusion"),

            [StatusEffectType.Fear] = new StatusEffectDefinition(
                StatusEffectType.Fear, "Fear", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.Strength, -1),
                    new StatModifierEntry(StatType.MagicPower, -1),
                    new StatModifierEntry(StatType.PhysicalDefense, -1),
                    new StatModifierEntry(StatType.MagicDefense, -1)
                }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0f, 0.5f), Description: "All stats reduced per stack"),

            [StatusEffectType.Bleeding] = new StatusEffectDefinition(
                StatusEffectType.Bleeding, "Bleeding", StackPolicy.StackIntensity,
                DamagePerTick: 1, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0f, 0f), Description: "Escalating damage each turn"),

            [StatusEffectType.Concentrated] = new StatusEffectDefinition(
                StatusEffectType.Concentrated, "Concentrated", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.9f, 0.4f), Description: "Building focus into strength"),

            [StatusEffectType.Growing] = new StatusEffectDefinition(
                StatusEffectType.Growing, "Growing", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.3f, 0.8f, 0.2f), Description: "Regenerating health each turn"),

            [StatusEffectType.Thorns] = new StatusEffectDefinition(
                StatusEffectType.Thorns, "Thorns", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0.6f, 0.1f), Description: "Returns damage to attackers"),

            [StatusEffectType.Reflecting] = new StatusEffectDefinition(
                StatusEffectType.Reflecting, "Reflecting", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.8f, 0.8f, 1f), Description: "Reflects single-target attacks"),

            [StatusEffectType.Hardening] = new StatusEffectDefinition(
                StatusEffectType.Hardening, "Hardening", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.4f, 0.3f), Description: "Reducing incoming damage"),

            [StatusEffectType.Drunk] = new StatusEffectDefinition(
                StatusEffectType.Drunk, "Drunk", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.85f, 0.2f), Description: "Scrambles keyboard letters"),
        };

        public static StatusEffectDefinition Get(StatusEffectType type) => All[type];
    }
}
