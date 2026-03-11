using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class FrostbittenHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Frostbitten;

        private readonly Dictionary<EntityId, (string magModId, int tickCount)> _tracking = new();

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            ctx.TurnService.MoveToLastInRound(target);

            if (ctx.StatusEffects.HasEffect(target, StatusEffectType.Burning))
            {
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Burning);
                ctx.StatusEffects.ApplyEffect(target, StatusEffectType.Wet, 2, target);
            }

            if (!_tracking.ContainsKey(target))
                _tracking[target] = (null, 0);
        }

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var (magModId, tickCount) = _tracking.TryGetValue(target, out var t) ? t : (null, 0);
            tickCount++;

            // Remove old modifier, add updated one
            if (magModId != null)
                ctx.EntityStats.RemoveModifier(target, StatType.MagicPower, magModId);

            var newModId = $"frostbitten_mag_{target.Value}_{tickCount}";
            var modifier = new StatusEffectModifier(newModId, -tickCount);
            ctx.EntityStats.AddModifier(target, StatType.MagicPower, modifier);
            _tracking[target] = (newModId, tickCount);

            instance.StackCount--;
            if (instance.StackCount <= 0)
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Frostbitten);
        }

        public override void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifier(target, ctx);
        }

        public override void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifier(target, ctx);
        }

        private void RemoveModifier(EntityId target, IStatusEffectHandlerContext ctx)
        {
            if (_tracking.TryGetValue(target, out var t) && t.magModId != null)
            {
                ctx.EntityStats.RemoveModifier(target, StatType.MagicPower, t.magModId);
                _tracking.Remove(target);
            }
        }
    }
}
