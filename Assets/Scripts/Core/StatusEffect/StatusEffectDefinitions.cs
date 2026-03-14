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
                DisplayColor: new Color(1f, 0.6f, 0f), Description: "Deals 3 fire damage at the start of each turn. Duration resets on reapply."),

            [StatusEffectType.Wet] = new StatusEffectDefinition(
                StatusEffectType.Wet, "Wet", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.3f, 0.7f, 1f), Description: "Shock attacks deal double damage. Removes Burning on apply."),

            [StatusEffectType.Poisoned] = new StatusEffectDefinition(
                StatusEffectType.Poisoned, "Poisoned", StackPolicy.StackIntensity,
                DamagePerTick: 2, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0.1f, 0.9f), Description: "Deals 2 damage per stack at the start of each turn. Stacks increase on reapply."),

            [StatusEffectType.Frozen] = new StatusEffectDefinition(
                StatusEffectType.Frozen, "Frozen", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.PhysicalDefense, 999),
                    new StatModifierEntry(StatType.MagicDefense, 999)
                }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.9f, 1f), Description: "Encased in ice. Cannot act. Defense set to maximum. Cannot be reapplied."),

            [StatusEffectType.Slowed] = new StatusEffectDefinition(
                StatusEffectType.Slowed, "Slowed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.MagicPower, -3) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0.6f, 0.8f), Description: "Magic Power reduced by 3."),

            [StatusEffectType.Cursed] = new StatusEffectDefinition(
                StatusEffectType.Cursed, "Cursed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Luck, -5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0f, 0.5f), Description: "Luck reduced by 5."),

            [StatusEffectType.Buffed] = new StatusEffectDefinition(
                StatusEffectType.Buffed, "Buffed", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.Strength, 5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.2f, 1f, 0.3f), Description: "Strength increased by 5.", IsPositive: true),

            [StatusEffectType.Shielded] = new StatusEffectDefinition(
                StatusEffectType.Shielded, "Shielded", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: new[] { new StatModifierEntry(StatType.PhysicalDefense, 5) }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0.6f, 0.6f), Description: "Physical Defense increased by 5.", IsPositive: true),

            [StatusEffectType.ExtraTurn] = new StatusEffectDefinition(
                StatusEffectType.ExtraTurn, "Extra Turn", StackPolicy.Ignore,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: true,
                DisplayColor: new Color(1f, 0.9f, 0.4f), Description: "Grants one additional turn this round.", IsPositive: true),

            [StatusEffectType.Stun] = new StatusEffectDefinition(
                StatusEffectType.Stun, "Stunned", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 1f, 0.2f), Description: "Skip next turn. Cannot act."),

            [StatusEffectType.Concussion] = new StatusEffectDefinition(
                StatusEffectType.Concussion, "Concussion", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.7f, 0.3f), Description: "Scrambles 1 keyboard letter per stack. 50% chance each turn to deal 1 damage and Stun. Permanent."),

            [StatusEffectType.Fear] = new StatusEffectDefinition(
                StatusEffectType.Fear, "Fear", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: new[]
                {
                    new StatModifierEntry(StatType.Strength, -1),
                    new StatModifierEntry(StatType.MagicPower, -1),
                    new StatModifierEntry(StatType.PhysicalDefense, -1),
                    new StatModifierEntry(StatType.MagicDefense, -1)
                }, GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0f, 0.5f), Description: "STR, MGC, PDEF, MDEF reduced by 1 per stack."),

            [StatusEffectType.Bleeding] = new StatusEffectDefinition(
                StatusEffectType.Bleeding, "Bleeding", StackPolicy.StackIntensity,
                DamagePerTick: 1, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0f, 0f), Description: "Deals 1 damage per stack each turn. Stacks grow by 1 each tick. Healing reduces stacks."),

            [StatusEffectType.Concentrated] = new StatusEffectDefinition(
                StatusEffectType.Concentrated, "Concentrated", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.9f, 0.4f), Description: "Converts all stacks to Strength on expire. +1 STR per stack.", IsPositive: true),

            [StatusEffectType.Growing] = new StatusEffectDefinition(
                StatusEffectType.Growing, "Growing", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.3f, 0.8f, 0.2f), Description: "Heals 1 HP per stack each turn. Double healing if Wet.", IsPositive: true),

            [StatusEffectType.Thorns] = new StatusEffectDefinition(
                StatusEffectType.Thorns, "Thorns", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.4f, 0.6f, 0.1f), Description: "When hit, reflects damage back to attacker. Loses 1 stack per hit.", IsPositive: true),

            [StatusEffectType.Reflecting] = new StatusEffectDefinition(
                StatusEffectType.Reflecting, "Reflecting", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.8f, 0.8f, 1f), Description: "Redirects single-target attacks back at the caster. Loses 1 stack per redirect.", IsPositive: true),

            [StatusEffectType.Hardening] = new StatusEffectDefinition(
                StatusEffectType.Hardening, "Hardening", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.4f, 0.3f), Description: "Reduces all incoming damage by stack count. Loses 1 stack each turn.", IsPositive: true),

            [StatusEffectType.Drunk] = new StatusEffectDefinition(
                StatusEffectType.Drunk, "Drunk", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.85f, 0.2f), Description: "Scrambles 1 keyboard letter per stack. Loses 1 stack each turn."),

            [StatusEffectType.Frostbitten] = new StatusEffectDefinition(
                StatusEffectType.Frostbitten, "Frostbitten", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.85f, 1f), Description: "Always act last. Magic Power reduced by 1 per stack each turn."),

            [StatusEffectType.Energetic] = new StatusEffectDefinition(
                StatusEffectType.Energetic, "Energetic", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.6f, 0f), Description: "Next word's actions trigger twice at half power. Applies Tired afterward.", IsPositive: true),

            [StatusEffectType.Tired] = new StatusEffectDefinition(
                StatusEffectType.Tired, "Tired", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.5f, 0.5f, 0.5f), Description: "STR and MGC reduced by 1 per stack. At 3+ stacks, may fall asleep."),

            [StatusEffectType.Sleep] = new StatusEffectDefinition(
                StatusEffectType.Sleep, "Sleep", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.2f, 0.2f, 0.6f), Description: "Cannot act. Taking damage has a 20% chance per stack to wake. Stacks reduce on wake."),

            [StatusEffectType.Anxiety] = new StatusEffectDefinition(
                StatusEffectType.Anxiety, "Anxiety", StackPolicy.StackIntensity,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.7f, 0.3f, 0.5f), Description: "Each turn, may auto-cast a random THOUGHTS word. Chance increases with stacks."),

            [StatusEffectType.Awakened] = new StatusEffectDefinition(
                StatusEffectType.Awakened, "Awakened", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(1f, 0.95f, 0.6f), Description: "All stats +1. Immune to Sleep, Tired, and Frostbitten.", IsPositive: true),

            [StatusEffectType.Silenced] = new StatusEffectDefinition(
                StatusEffectType.Silenced, "Silenced", StackPolicy.RefreshDuration,
                DamagePerTick: null, StatModifiers: System.Array.Empty<StatModifierEntry>(), GrantsExtraTurn: false,
                DisplayColor: new Color(0.6f, 0.4f, 0.7f), Description: "Cannot receive new status effects. Existing effects still tick."),
        };

        public static StatusEffectDefinition Get(StatusEffectType type) => All[type];
    }
}
