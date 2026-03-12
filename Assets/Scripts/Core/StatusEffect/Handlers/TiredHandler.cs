using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.StatusEffect.Handlers
{
    [AutoScan]
    internal sealed class TiredHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Tired;

        private readonly Dictionary<EntityId, (string strModId, string magModId)> _modifiers = new();

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            UpdateModifiers(target, instance.StackCount, ctx);
        }

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            instance.StackCount--;
            if (instance.StackCount <= 0)
            {
                ctx.StatusEffects.RemoveEffect(target, StatusEffectType.Tired);
                return;
            }

            UpdateModifiers(target, instance.StackCount, ctx);

            // Chance to proc Sleep: 10% per stack, reduced by luck
            float sleepChance = 10 * instance.StackCount / 100f;
            if (ctx.LuckService != null)
                sleepChance = ctx.LuckService.AdjustChance(sleepChance, target, false);
            if (Random.value < sleepChance)
                ctx.StatusEffects.ApplyEffect(target, StatusEffectType.Sleep,
                    StatusEffectInstance.PermanentDuration, target);
        }

        public override void OnRemove(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifiers(target, ctx);
        }

        public override void OnExpire(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            RemoveModifiers(target, ctx);
        }

        private void UpdateModifiers(EntityId target, int stacks, IStatusEffectHandlerContext ctx)
        {
            RemoveModifiers(target, ctx);

            var strModId = $"tired_str_{target.Value}";
            var magModId = $"tired_mag_{target.Value}";
            ctx.EntityStats.AddModifier(target, StatType.Strength, new StatusEffectModifier(strModId, -stacks));
            ctx.EntityStats.AddModifier(target, StatType.MagicPower, new StatusEffectModifier(magModId, -stacks));
            _modifiers[target] = (strModId, magModId);
        }

        private void RemoveModifiers(EntityId target, IStatusEffectHandlerContext ctx)
        {
            if (_modifiers.TryGetValue(target, out var mods))
            {
                ctx.EntityStats.RemoveModifier(target, StatType.Strength, mods.strModId);
                ctx.EntityStats.RemoveModifier(target, StatType.MagicPower, mods.magModId);
                _modifiers.Remove(target);
            }
        }
    }
}
