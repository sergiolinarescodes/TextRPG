using System.Collections.Generic;
using UnityEngine;

namespace TextRPG.Core.WordAction
{
    internal static class ActionDescriptions
    {
        private static readonly Dictionary<string, string> TargetAliases = new()
        {
            ["SingleEnemy"] = "Enemy",
            ["RandomEnemy"] = "Random Enemy",
            ["AllEnemies"] = "All Enemies",
            ["AllAllies"] = "All Allies",
            ["AllAlliesAndSelf"] = "All Allies & Self",
            ["FrontEnemy"] = "Front",
            ["MiddleEnemy"] = "Middle",
            ["BackEnemy"] = "Back",
            ["LowestHealthEnemy"] = "Weakest",
            ["HighestHealthEnemy"] = "Strongest",
            ["RandomAlly"] = "Random Ally",
            ["RandomAny"] = "Random",
            ["TwoRandomEnemies"] = "2 Random",
            ["HalfEnemiesRandom"] = "Half Enemies",
            ["All"] = "All",
            ["Self"] = "Self",
        };

        public static string FormatAction(WordActionMapping mapping)
        {
            var text = $"{mapping.ActionId} {mapping.Value}";
            if (mapping.Target != null)
            {
                var target = TargetAliases.TryGetValue(mapping.Target, out var alias) ? alias : mapping.Target;
                text += $" ({target})";
            }
            return text;
        }

        public static string FormatNatural(WordActionMapping mapping)
        {
            var v = mapping.Value;
            var text = mapping.ActionId switch
            {
                "Damage" => $"Deals {v} damage",
                "MagicDamage" => $"Deals {v} magic damage",
                "WeaponDamage" => $"Deals {v} weapon damage",
                "Heal" => $"Heals {v}",
                "Burn" => $"Burns for {v}",
                "Wet" => $"Wets ({v})",

                "Shock" => $"Shocks for {v}",
                "Fear" => $"Inflicts Fear ({v})",
                "Stun" => $"Stuns ({v})",
                "Freeze" => $"Frostbites ({v})",
                "Concussion" => $"Concusses ({v})",

                "Poison" => $"Poisons ({v})",
                "Bleed" => $"Bleeds ({v})",
                "Drunk" => $"Drunk ({v} stacks)",
                "Concentrate" => $"Concentrates ({v})",
                "Grow" => $"Grows ({v})",
                "Thorns" => $"Thorns ({v})",
                "Reflect" => $"Reflects ({v})",
                "Hardening" => $"Hardens ({v})",
                "Energize" => $"Energized ({v})",
                "Relax" => "Relaxes",
                "Sleep" => $"Sleeps ({v})",
                "RestHeal" => $"Rest heals {v}",
                "Melt" => $"Melts ({v})",
                "Sunder" => $"Strips {v} buffs (+1 dmg each)",
                "Silence" => $"Silences ({v})",
                "Ignite" => $"Ignites ({v})",
                "Combust" => $"Combusts ({v})",
                "BuffStrength" => $"+{v} STR",
                "BuffMagicPower" => $"+{v} MAG",
                "BuffPhysicalDefense" => $"+{v} DEF",
                "BuffMagicDefense" => $"+{v} MDEF",
                "BuffLuck" => $"+{v} LCK",
                "DebuffStrength" => $"-{v} STR",
                "DebuffMagicPower" => $"-{v} MAG",
                "DebuffPhysicalDefense" => $"-{v} DEF",
                "DebuffMagicDefense" => $"-{v} MDEF",
                "DebuffLuck" => $"-{v} LCK",
                "Summon" => string.IsNullOrEmpty(mapping.AssocWord)
                    ? "Summons ally"
                    : $"Summons {mapping.AssocWord.ToUpperInvariant()}",
                "Shield" => $"Shields {v}",
                "Mana" => $"Restores {v} mana",
                _ => $"{mapping.ActionId} {v}"
            };

            if (mapping.Target != null)
            {
                var target = TargetAliases.TryGetValue(mapping.Target, out var alias) ? alias : mapping.Target;
                text = $"{text} ({target})";
            }

            return text;
        }

        public static Color GetColor(string actionId, IActionRegistry registry)
        {
            if (registry != null && registry.TryGet(actionId, out var def))
                return def.Color;
            return Color.gray;
        }
    }
}
