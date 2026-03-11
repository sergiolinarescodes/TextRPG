using TextRPG.Core.Services;

namespace TextRPG.Core.Effects.Definitions
{
    [AutoScan]
    internal sealed class DamageGameEffect : IGameEffect
    {
        public string EffectId => "damage";

        public void Execute(EffectContext context)
        {
            var stats = context.Services.EntityStats;
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                if (!stats.HasEntity(target) || stats.GetCurrentHealth(target) <= 0) continue;
                stats.ApplyDamage(target, context.Value, context.Source);
            }
        }
    }
}
