using TextRPG.Core.StatusEffect;
using Unidad.Core.Patterns.Scoring;

namespace TextRPG.Core.EnemyAI.Contributors
{
    internal sealed class StatusEffectScoreContributor : IContributor<AIDecisionContext>
    {
        private readonly IStatusEffectService _statusEffects;

        public string Id => "status_effect_score";
        public float Weight => 0.8f;

        public StatusEffectScoreContributor(IStatusEffectService statusEffects)
        {
            _statusEffects = statusEffects;
        }

        public float Evaluate(AIDecisionContext context)
        {
            var word = context.AbilityWord.ToLowerInvariant();

            if (word == "shout" && _statusEffects.HasEffect(context.TargetId, StatusEffectType.Fear))
                return -2f;

            return 0f;
        }
    }
}
