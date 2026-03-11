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
            // Enemies always prefer real abilities over scratch.
            // Scratch is only used as a mana-starved fallback.
            if (context.AbilityWord == "scratch")
                return -1f;

            // Small random for variety when choosing between multiple real abilities.
            return 2f + (float)Rng.NextDouble() * 0.5f;
        }
    }
}
