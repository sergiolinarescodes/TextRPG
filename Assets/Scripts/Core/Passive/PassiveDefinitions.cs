using System.Collections.Generic;
using UnityEngine;

namespace TextRPG.Core.Passive
{
    public sealed record PassiveDefinition(string DisplayName, string Description, Color DisplayColor);

    public static class PassiveDefinitions
    {
        private static readonly Dictionary<string, Color> EffectColors = new()
        {
            ["heal"] = Color.green,
            ["damage"] = Color.red,
            ["shield"] = Color.cyan,
            ["mana"] = new Color(0.3f, 0.5f, 1f),
            ["apply_status"] = Color.yellow,
        };

        private static readonly Dictionary<string, string> TriggerNames = new()
        {
            ["on_ally_hit"] = "When ally hit",
            ["on_self_hit"] = "When hit",
            ["on_round_end"] = "Each round end",
            ["on_round_start"] = "Each round start",
            ["on_turn_start"] = "On turn start",
            ["on_turn_end"] = "On turn end",
            ["on_word_played"] = "On word played",
            ["on_word_length"] = "On long word",
            ["on_word_tag"] = "On tagged word",
            ["on_kill"] = "On kill",
            [PassiveConstants.Taunt] = "Taunt",
        };

        private static readonly Dictionary<string, string> EffectNames = new()
        {
            ["heal"] = "heal",
            ["damage"] = "damage",
            ["shield"] = "shield",
            ["mana"] = "restore mana",
            ["apply_status"] = "apply status",
        };

        private static readonly Dictionary<string, string> TargetNames = new()
        {
            ["Self"] = "self",
            ["AllAllies"] = "all allies",
            ["AllEnemies"] = "all enemies",
            ["Injured"] = "injured ally",
            ["Attacker"] = "attacker",
        };

        public static PassiveDefinition Generate(Encounter.PassiveEntry entry)
        {
            if (entry.TriggerId == PassiveConstants.Taunt)
                return new PassiveDefinition("Taunt", "Forces enemies to target this unit", Color.yellow);

            var trigger = TriggerNames.TryGetValue(entry.TriggerId, out var t) ? t : entry.TriggerId;
            var effect = EffectNames.TryGetValue(entry.EffectId, out var e) ? e : entry.EffectId;
            var target = TargetNames.TryGetValue(entry.Target, out var tgt) ? tgt : entry.Target;
            var color = EffectColors.TryGetValue(entry.EffectId, out var c) ? c : Color.gray;

            var triggerText = trigger;
            if (entry.TriggerId == "on_word_length" && entry.TriggerParam != null)
                triggerText = $"{trigger} ({entry.TriggerParam}+ letters)";
            else if (entry.TriggerId == "on_word_tag" && entry.TriggerParam != null)
                triggerText = $"{trigger} ({entry.TriggerParam})";

            var effectText = entry.EffectId == "apply_status" && entry.EffectParam != null
                ? $"apply {entry.EffectParam}"
                : $"{effect} {entry.Value}";

            var description = $"{triggerText}: {effectText} to {target}";

            return new PassiveDefinition(effect, description, color);
        }
    }
}
