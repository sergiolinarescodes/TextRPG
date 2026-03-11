using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using Unidad.Core.EventBus;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnAllyHitTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_ally_hit";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                if (evt.EntityId.Equals(owner)) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, evt.EntityId, ctx)) return;

                onTriggered(new PassiveTriggerContext(owner, evt.EntityId, evt.DamageSource, null));
            });
        }
    }
}
