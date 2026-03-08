using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class ConcentratedHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Concentrated;

        private readonly Dictionary<EntityId, string> _modifierIds = new();

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            if (instance.StackCount > 0)
            {
                var modId = $"concentrated_str_{target.Value}";
                var modifier = new StatusEffectModifier(modId, instance.StackCount);
                ctx.EntityStats.AddModifier(target, StatType.Strength, modifier);
                _modifierIds[target] = modId;
                instance.StackCount = 0;
            }
        }

        public override void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifier(target, ctx);
        }

        public override void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifier(target, ctx);
        }

        private void RemoveModifier(EntityId target, IStatusEffectHandlerContext ctx)
        {
            if (_modifierIds.TryGetValue(target, out var modId))
            {
                ctx.EntityStats.RemoveModifier(target, StatType.Strength, modId);
                _modifierIds.Remove(target);
            }
        }
    }
}
