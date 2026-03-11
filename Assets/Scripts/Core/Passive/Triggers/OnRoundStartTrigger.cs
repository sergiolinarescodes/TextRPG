using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnRoundStartTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_round_start";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            return ctx.EventBus.Subscribe<RoundStartedEvent>(_ =>
            {
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;

                onTriggered(new PassiveTriggerContext(owner, null, null, null));
            });
        }
    }
}
