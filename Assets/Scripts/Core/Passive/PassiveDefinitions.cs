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
            ["steal_stat"] = new Color(0.6f, 0.2f, 0.8f),
            ["gold"] = new Color(0.8f, 0.65f, 0.2f),
            ["buff_stat"] = new Color(0.4f, 0.9f, 0.4f),
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
            ["on_ally_death"] = "On ally death",
            ["on_death"] = "On death",
            ["on_damage_dealt"] = "On damage dealt",
            ["on_letter_in_word"] = "On letter match",
            [PassiveConstants.Taunt] = "Taunt",
        };

        private static readonly Dictionary<string, string> EffectNames = new()
        {
            ["heal"] = "heal",
            ["damage"] = "damage",
            ["shield"] = "shield",
            ["mana"] = "restore mana",
            ["apply_status"] = "apply status",
            ["steal_stat"] = "steal stat",
            ["gold"] = "earn gold",
            ["buff_stat"] = "buff stat",
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
            else if (entry.TriggerId == "on_letter_in_word" && entry.TriggerParam != null)
            {
                var modeDesc = entry.TriggerParam switch
                {
                    "vowel" => "a random vowel",
                    "consonant" => "a random consonant",
                    _ when entry.TriggerParam.StartsWith("fixed:") => $"the letter '{entry.TriggerParam.Split(':')[1].ToUpper()}'",
                    _ when entry.TriggerParam.StartsWith("multi:") => $"the letters '{entry.TriggerParam.Split(':')[1].ToUpper()}'",
                    _ => $"a random letter",
                };

                var effectDesc = entry.EffectId == "buff_stat" && entry.EffectParam != null
                    ? $"gain +{entry.Value} {entry.EffectParam}"
                    : $"{effect} {entry.Value} to {target}";

                var fullDesc = $"Each turn, {modeDesc} is selected. Type a word containing it to {effectDesc} (permanent)";
                return new PassiveDefinition(effect, fullDesc, color);
            }
            else if (entry.TriggerId == "on_death" && entry.TriggerParam == "siphon")
                return new PassiveDefinition("Release", "On death: release absorbed stats as healing to allies",
                    new Color(0.6f, 0.2f, 0.8f));

            string effectText;
            if (entry.EffectId == "apply_status" && entry.EffectParam != null)
                effectText = $"apply {entry.EffectParam}";
            else if (entry.EffectId == "buff_stat" && entry.EffectParam != null)
                effectText = $"+{entry.Value} {entry.EffectParam}";
            else
                effectText = $"{effect} {entry.Value}";

            var description = $"{triggerText}: {effectText} to {target}";

            return new PassiveDefinition(effect, description, color);
        }
    }
}
