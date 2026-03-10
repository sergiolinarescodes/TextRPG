using System;
using Unidad.Core.Patterns.Scoring;

namespace TextRPG.Core.CombatAI.Contributors
{
    internal sealed class AbilityPreferenceContributor : IContributor<AIDecisionContext>
    {
        private static readonly Random Rng = new();

        public string Id => "ability_preference";
        public float Weight => 1f;

        public float Evaluate(AIDecisionContext context)
        {
            // Add random variety to ability selection.
            // Mana abilities get a small bonus so they're preferred ~60-65% of the time,
            // but basic attacks still happen regularly for natural combat feel.
            var random = (float)Rng.NextDouble();
            return context.ManaCost > 0 ? random + 0.15f : random;
        }
    }
}
