using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class BurningHandler : BaseStatusEffectHandler
    {
        public override StatusEffectType EffectType => StatusEffectType.Burning;

        public override void OnApply(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
        }

        public override void OnTick(EntityId target, StatusEffectInstance instance, IStatusEffectHandlerContext ctx)
        {
            var definition = StatusEffectDefinitions.Get(StatusEffectType.Burning);
            if (definition.DamagePerTick.HasValue)
            {
                var damage = definition.DamagePerTick.Value * instance.StackCount;

                var encounterSvc = ctx.EncounterService;
                if (encounterSvc != null)
                {
                    var entityDef = encounterSvc.GetEntityDefinition(target);
                    if (entityDef?.Tags != null && Array.IndexOf(entityDef.Tags, "flammable") >= 0)
                        damage *= 2;
                }

                ctx.EntityStats.ApplyDamage(target, damage, instance.Source);
                ctx.EventBus.Publish(new StatusEffectDamageEvent(target, StatusEffectType.Burning, damage));
            }
        }
    }
}
