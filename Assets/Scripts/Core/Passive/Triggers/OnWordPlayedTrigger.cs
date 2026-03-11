using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnWordPlayedTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_word_played";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            EntityId currentTurnEntity = default;
            var turnSub = ctx.EventBus.Subscribe<TurnStartedEvent>(e => currentTurnEntity = e.EntityId);
            var wordSub = ctx.EventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                if (e.Word.Length == 0) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, currentTurnEntity, ctx)) return;

                onTriggered(new PassiveTriggerContext(owner, null, currentTurnEntity, e.Word));
            });

            var composite = new CompositeDisposable();
            composite.Add(turnSub);
            composite.Add(wordSub);
            return composite;
        }
    }
}
