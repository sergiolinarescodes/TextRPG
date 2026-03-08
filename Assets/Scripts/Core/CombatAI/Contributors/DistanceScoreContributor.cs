using Unidad.Core.Patterns.Scoring;

namespace TextRPG.Core.CombatAI.Contributors
{
    internal sealed class DistanceScoreContributor : IContributor<AIDecisionContext>
    {
        public string Id => "distance_score";
        public float Weight => 1.5f;

        public float Evaluate(AIDecisionContext context)
        {
            // In slot system, everything is always in range
            return 2f;
        }
    }
}
