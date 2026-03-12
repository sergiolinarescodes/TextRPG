using static TextRPG.Core.ActionExecution.ActionNames;

namespace TextRPG.Core.ActionExecution
{
    internal static class ActionTemplateDefTable
    {
        public static readonly ActionTemplateDef[] Definitions =
        {
            // Scaled damage
            new(Damage, "scaled_damage", "Strength", "PhysicalDefense"),
            new(Smash, "scaled_damage", "Strength", "PhysicalDefense"),
            new(MagicDamage, "scaled_damage", "MagicPower", "MagicDefense"),
            new(Cataclysm, "scaled_damage", "MagicPower", "MagicDefense"),
            new(WeaponDamage, "scaled_damage", "Dexterity", "PhysicalDefense"),

            // Support
            new(Heal, "heal"),
            new(Shield, "shield"),
            new(Thinking, "mana_self"),

            // Utility
            new(Pay, "noop"),
            new(Push, "push"),

            // Status effects (applied to targets)
            new(Burn, "apply_status", "Burning", "FromValue"),
            new(Water, "apply_status", "Wet", "FromValue"),
            new(Fear, "apply_status", "Fear", "FromValue"),
            new(Stun, "apply_status", "Stun", "FromValue"),
            new(Poison, "apply_status", "Poisoned", "FromValue"),
            new(Bleed, "apply_status", "Bleeding", "Permanent"),

            // Status effects (applied to self)
            new(Grow, "apply_status", "Growing", "FromValue", ApplySelf: true),
            new(Thorns, "apply_status", "Thorns", "FromValue", ApplySelf: true),
            new(Reflect, "apply_status", "Reflecting", "StackByValue", ApplySelf: true),
            new(Hardening, "apply_status", "Hardening", "StackByValue", ApplySelf: true),
            new(Drunk, "apply_status", "Drunk", "StackByValue", ApplySelf: true),
            new(Freeze, "apply_status", "Frostbitten", "StackByValue"),
            new(Energize, "apply_status", "Energetic", "FromValue", ApplySelf: true),
            new(SleepAction, "apply_status", "Sleep", "StackByValue", ApplySelf: true),

            // Status effects (applied to target — non-self)
            new(Comfort, "apply_status", "Energetic", "FromValue"),

            // Noop (tag-driven)
            new(Relax, "noop"),

            // Stat modifiers — buffs
            new("BuffStrength", "stat_modifier", "Strength", "buff"),
            new("BuffMagicPower", "stat_modifier", "MagicPower", "buff"),
            new("BuffPhysicalDefense", "stat_modifier", "PhysicalDefense", "buff"),
            new("BuffMagicDefense", "stat_modifier", "MagicDefense", "buff"),
            new("BuffLuck", "stat_modifier", "Luck", "buff"),
            new("BuffMaxMana", "stat_modifier", "MaxMana", "buff"),
            new("BuffManaRegen", "stat_modifier", "ManaRegen", "buff"),
            new(Buff, "stat_modifier", "Strength", "buff"),

            // Stat modifiers — debuffs
            new("DebuffStrength", "stat_modifier", "Strength", "debuff"),
            new("DebuffMagicPower", "stat_modifier", "MagicPower", "debuff"),
            new("DebuffPhysicalDefense", "stat_modifier", "PhysicalDefense", "debuff"),
            new("DebuffMagicDefense", "stat_modifier", "MagicDefense", "debuff"),
            new("DebuffLuck", "stat_modifier", "Luck", "debuff"),
            new("DebuffMaxMana", "stat_modifier", "MaxMana", "debuff"),
            new("DebuffManaRegen", "stat_modifier", "ManaRegen", "debuff"),
            new(Melt, "stat_modifier", "PhysicalDefense", "debuff"),

            // CriticalDamage modifiers
            new("BuffCriticalDamage", "stat_modifier", "CriticalDamage", "buff"),
            new("DebuffCriticalDamage", "stat_modifier", "CriticalDamage", "debuff"),
        };
    }
}
