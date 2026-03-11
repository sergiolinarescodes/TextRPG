using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class AwakenedHandler : BaseStatusEffectHandler
    {
        private static readonly StatType[] AllStats =
        {
            StatType.Strength,
            StatType.MagicPower,
            StatType.PhysicalDefense,
            StatType.MagicDefense,
            StatType.Luck,
        };

        public override StatusEffectType EffectType => StatusEffectType.Awakened;

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            for (int i = 0; i < AllStats.Length; i++)
            {
                var modId = $"awakened_{AllStats[i]}_{target}";
                ctx.EntityStats.AddModifier(target, AllStats[i], new StatBuffModifier(modId, 1));
            }
        }

        public override void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifiers(target, ctx);
        }

        public override void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifiers(target, ctx);
        }

        private static void RemoveModifiers(EntityId target, IStatusEffectHandlerContext ctx)
        {
            for (int i = 0; i < AllStats.Length; i++)
            {
                var modId = $"awakened_{AllStats[i]}_{target}";
                ctx.EntityStats.RemoveModifier(target, AllStats[i], modId);
            }
        }
    }
}
