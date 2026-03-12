using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnAllyDeathTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_ally_death";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                if (evt.RemainingHealth != 0) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (evt.EntityId.Equals(owner)) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, evt.EntityId, ctx)) return;

                onTriggered(new PassiveTriggerContext(owner, evt.EntityId, evt.DamageSource, null));
            });
        }
    }
}
