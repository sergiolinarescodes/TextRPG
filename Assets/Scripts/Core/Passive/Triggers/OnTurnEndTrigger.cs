using System;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnTurnEndTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_turn_end";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<TurnEndedEvent>(evt =>
            {
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;

                if (triggerParam == "self")
                {
                    if (!evt.EntityId.Equals(owner)) return;
                }
                else if (triggerParam == "any")
                {
                    // triggers for any entity's turn
                }
                else
                {
                    // default: faction-scoped
                    if (!PassiveTargetResolver.IsSameFaction(owner, evt.EntityId, ctx)) return;
                }

                onTriggered(new PassiveTriggerContext(owner, evt.EntityId, null, null));
            });
        }
    }
}
