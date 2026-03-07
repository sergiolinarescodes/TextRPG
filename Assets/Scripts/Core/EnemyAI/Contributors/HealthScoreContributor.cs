using Unidad.Core.Patterns.Scoring;

namespace TextRPG.Core.EnemyAI.Contributors
{
    internal sealed class HealthScoreContributor : IContributor<AIDecisionContext>
    {
        public string Id => "health_score";
        public float Weight => 1f;

        public float Evaluate(AIDecisionContext context)
        {
            var targetRatio = context.TargetMaxHealth > 0
                ? (float)context.TargetHealth / context.TargetMaxHealth
                : 0f;

            return 1f - targetRatio;
        }
    }
}
