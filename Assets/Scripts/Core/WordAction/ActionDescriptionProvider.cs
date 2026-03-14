using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    internal static class ActionDescriptionProvider
    {
        private static readonly Dictionary<string, string> Descriptions = new()
        {
            ["Damage"] = "Deals physical damage to the target",
            ["MagicDamage"] = "Deals magical damage to the target",
            ["WeaponDamage"] = "Deals weapon damage, scaling with dexterity",
            ["Heal"] = "Restores health points",
            ["Burn"] = "Sets the target ablaze, dealing fire damage each turn",
            ["Wet"] = "Soaks the target, increasing shock vulnerability",

            ["Shock"] = "Electrocutes the target with lightning",
            ["Fear"] = "Inflicts terror, may cause the target to skip a turn",
            ["Stun"] = "Stuns the target, preventing action for a duration",
            ["Freeze"] = "Encases the target in ice, slowing or immobilizing them",
            ["Concussion"] = "Dazes the target, reducing accuracy",

            ["Poison"] = "Poisons the target, dealing damage over time",
            ["Bleed"] = "Causes bleeding, dealing damage each turn",
            ["Drunk"] = "Scrambles keyboard input letters",
            ["Concentrate"] = "Focus energy, boosting next action",
            ["Grow"] = "Increases in size and power over time",
            ["Thorns"] = "Reflects a portion of damage back to attackers",
            ["Reflect"] = "Creates a barrier that reflects incoming damage",
            ["Hardening"] = "Hardens defenses, reducing incoming damage",
            ["BuffStrength"] = "Increases physical attack power",
            ["BuffMagicPower"] = "Increases magical attack power",
            ["BuffPhysicalDefense"] = "Increases physical defense",
            ["BuffMagicDefense"] = "Increases magical defense",
            ["BuffLuck"] = "Increases luck stat",
            ["DebuffStrength"] = "Reduces the target's attack power",
            ["DebuffMagicPower"] = "Reduces the target's magical power",
            ["DebuffPhysicalDefense"] = "Reduces the target's physical defense",
            ["DebuffMagicDefense"] = "Reduces the target's magical defense",
            ["DebuffLuck"] = "Reduces the target's luck",
            ["Melt"] = "Melts materials with intense heat, reducing physical defense",
            ["Summon"] = "Summons an ally to fight alongside you",
            ["Shield"] = "Grants a protective shield that absorbs damage",
            ["Mana"] = "Restores mana points",
            ["Ignite"] = "Scorches the target with fire, inflicting burns",
            ["Combust"] = "Detonates burning status for massive fire damage",
            ["Sunder"] = "Strips positive effects from the target, dealing damage per buff removed",
            ["Silence"] = "Prevents the target from receiving new status effects",
            ["Smash"] = "Heavy physical damage, crushes defenses",
        };

        public static string Get(string actionId) =>
            Descriptions.TryGetValue(actionId, out var desc) ? desc : null;

        public static bool Has(string actionId) => Descriptions.ContainsKey(actionId);
    }
}
