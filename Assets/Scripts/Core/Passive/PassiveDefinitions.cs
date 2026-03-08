using System.Collections.Generic;
using UnityEngine;

namespace TextRPG.Core.Passive
{
    public sealed record PassiveDefinition(string PassiveId, string DisplayName, string Description, Color DisplayColor);

    public static class PassiveDefinitions
    {
        private static readonly Dictionary<string, PassiveDefinition> All = new()
        {
            [PassiveIds.HealOnAllyHit] = new PassiveDefinition(
                PassiveIds.HealOnAllyHit, "Ally Protection", "Heals allies when they take damage", Color.green),

            [PassiveIds.HealOnRoundEnd] = new PassiveDefinition(
                PassiveIds.HealOnRoundEnd, "Regeneration Aura", "Heals all allies at end of each round", Color.green),

            [PassiveIds.DamageOnAllyHit] = new PassiveDefinition(
                PassiveIds.DamageOnAllyHit, "Retribution", "Damages attackers when allies are hit", Color.red),
        };

        private static readonly PassiveDefinition Fallback = new(
            "", "Unknown Passive", "Unknown effect", Color.gray);

        public static PassiveDefinition Get(string passiveId) =>
            All.TryGetValue(passiveId, out var def) ? def : Fallback;
    }
}
