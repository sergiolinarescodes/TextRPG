using System;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    internal sealed class OnSelfHitTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_self_hit";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                if (!evt.EntityId.Equals(owner)) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;

                onTriggered(new PassiveTriggerContext(owner, evt.EntityId, evt.DamageSource, null));
            });
        }
    }
}
