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
            ["FrontEnemy"] = "Front",
            ["MiddleEnemy"] = "Middle",
            ["BackEnemy"] = "Back",
            ["LowestHpEnemy"] = "Weakest Enemy",
            ["HighestHpEnemy"] = "Strongest Enemy",
            ["LowestHpAlly"] = "Weakest Ally",
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
            return mapping.ActionId switch
            {
                "Damage" => $"Deals {v} damage",
                "MagicDamage" => $"Deals {v} magic damage",
                "WeaponDamage" => $"Deals {v} weapon damage",
                "Heal" => $"Heals {v}",
                "Burn" => $"Burns for {v}",
                "Wet" => $"Wets ({v})",
                "Fire" => $"Fire blast {v}",
                "Shock" => $"Shocks for {v}",
                "Fear" => $"Inflicts Fear ({v})",
                "Stun" => $"Stuns ({v})",
                "Freeze" => $"Freezes ({v})",
                "Concussion" => $"Concusses ({v})",
                "Push" => $"Pushes {v}",
                "Poison" => $"Poisons ({v})",
                "Bleed" => $"Bleeds ({v})",
                "Drunk" => $"Drunk ({v} stacks)",
                "Concentrate" => $"Concentrates ({v})",
                "Grow" => $"Grows ({v})",
                "Thorns" => $"Thorns ({v})",
                "Reflect" => $"Reflects ({v})",
                "Hardening" => $"Hardens ({v})",
                "Melt" => $"Melts ({v})",
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
        }

        public static Color GetColor(string actionId, IActionRegistry registry)
        {
            if (registry != null && registry.TryGet(actionId, out var def))
                return def.Color;
            return Color.gray;
        }
    }
}
