using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnWordLengthTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_word_length";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            int minLength = 1;
            if (triggerParam != null) int.TryParse(triggerParam, out minLength);

            EntityId currentTurnEntity = default;
            var turnSub = ctx.EventBus.Subscribe<TurnStartedEvent>(e => currentTurnEntity = e.EntityId);
            var wordSub = ctx.EventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                if (e.Word.Length < minLength) return;
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
