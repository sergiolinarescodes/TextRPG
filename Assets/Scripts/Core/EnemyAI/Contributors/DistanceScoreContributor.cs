using Unidad.Core.Patterns.Scoring;

namespace TextRPG.Core.EnemyAI.Contributors
{
    internal sealed class DistanceScoreContributor : IContributor<AIDecisionContext>
    {
        public string Id => "distance_score";
        public float Weight => 1.5f;

        public float Evaluate(AIDecisionContext context)
        {
            if (context.IsMelee)
            {
                return context.Distance <= 1 ? 2f : -1f;
            }

            return 0.5f;
        }
    }
}
