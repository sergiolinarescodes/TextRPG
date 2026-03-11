using TextRPG.Core.Services;

namespace TextRPG.Core.Effects.Definitions
{
    [AutoScan]
    internal sealed class ManaGameEffect : IGameEffect
    {
        public string EffectId => "mana";

        public void Execute(EffectContext context)
        {
            var stats = context.Services.EntityStats;
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                if (!stats.HasEntity(target)) continue;
                stats.ApplyMana(target, context.Value);
            }
        }
    }
}
