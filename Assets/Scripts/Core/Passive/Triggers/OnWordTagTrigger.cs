using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    internal sealed class OnWordTagTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_word_tag";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            EntityId currentTurnEntity = default;
            var turnSub = ctx.EventBus.Subscribe<TurnStartedEvent>(e => currentTurnEntity = e.EntityId);
            var wordSub = ctx.EventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                if (e.Word.Length == 0) return;
                if (triggerParam == null) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (ctx.TagResolver == null || !ctx.TagResolver.HasTag(e.Word, triggerParam)) return;
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
