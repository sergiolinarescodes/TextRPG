using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using Unidad.Core.EventBus;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnKillTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_kill";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (evt.EntityId.Equals(owner)) return;
                if (PassiveTargetResolver.IsSameFaction(owner, evt.EntityId, ctx)) return;

                onTriggered(new PassiveTriggerContext(owner, evt.EntityId, null, null));
            });
        }
    }
}
