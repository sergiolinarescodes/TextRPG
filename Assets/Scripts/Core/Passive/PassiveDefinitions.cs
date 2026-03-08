using System.Collections.Generic;
using UnityEngine;

namespace TextRPG.Core.Passive
{
    public sealed record PassiveDefinition(string PassiveId, string DisplayName, string Description, Color DisplayColor);

    public static class PassiveDefinitions
    {
        private static readonly Dictionary<string, PassiveDefinition> All = new()
        {
            ["heal_on_ally_hit"] = new PassiveDefinition(
                "heal_on_ally_hit", "Ally Protection", "Heals allies when they take damage", Color.green),

            ["heal_on_round_end"] = new PassiveDefinition(
                "heal_on_round_end", "Regeneration Aura", "Heals all allies at end of each round", Color.green),

            ["damage_on_ally_hit"] = new PassiveDefinition(
                "damage_on_ally_hit", "Retribution", "Damages attackers when allies are hit", Color.red),
        };

        private static readonly PassiveDefinition Fallback = new(
            "", "Unknown Passive", "Unknown effect", Color.gray);

        public static PassiveDefinition Get(string passiveId) =>
            All.TryGetValue(passiveId, out var def) ? def : Fallback;
    }
}
