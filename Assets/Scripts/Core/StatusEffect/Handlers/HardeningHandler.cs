using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class HardeningHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Hardening;

        private readonly Dictionary<EntityId, string> _modifierIds = new();

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            UpdateModifier(target, instance.StackCount, ctx);
        }

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            instance.StackCount--;
            if (instance.StackCount <= 0)
            {
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Hardening);
            }
            else
            {
                UpdateModifier(target, instance.StackCount, ctx);
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

        private void UpdateModifier(EntityId target, int value, IStatusEffectHandlerContext ctx)
        {
            RemoveModifier(target, ctx);
            var modId = $"hardening_dmgred_{target.Value}";
            var modifier = new StatusEffectModifier(modId, value);
            ctx.EntityStats.AddModifier(target, StatType.DamageReduction, modifier);
            _modifierIds[target] = modId;
        }

        private void RemoveModifier(EntityId target, IStatusEffectHandlerContext ctx)
        {
            if (_modifierIds.TryGetValue(target, out var modId))
            {
                ctx.EntityStats.RemoveModifier(target, StatType.DamageReduction, modId);
                _modifierIds.Remove(target);
            }
        }
    }
}
