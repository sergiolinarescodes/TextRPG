using System;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.LetterChallenge;
using TextRPG.Core.Services;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive.Triggers
{
    [AutoScan]
    internal sealed class OnLetterInWordTrigger : IPassiveTrigger
    {
        public string TriggerId => "on_letter_in_word";

        public IDisposable Subscribe(EntityId owner, string triggerParam, IPassiveContext ctx,
                                      Action<PassiveTriggerContext> onTriggered)
        {
            var service = ctx.LetterChallengeService;
            if (service == null) return new CompositeDisposable();

            var mode = triggerParam ?? "vowel";
            EntityId currentTurnEntity = default;

            // Select initial letters
            service.SelectLetters(owner, mode);

            var turnSub = ctx.EventBus.Subscribe<TurnStartedEvent>(e =>
            {
                currentTurnEntity = e.EntityId;

                // Re-select letters each turn for same-faction turns
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, e.EntityId, ctx)) return;

                service.SelectLetters(owner, mode);
            });

            var wordSub = ctx.EventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                if (e.Word.Length == 0) return;
                if (!ctx.EntityStats.HasEntity(owner) || ctx.EntityStats.GetCurrentHealth(owner) <= 0) return;
                if (!PassiveTargetResolver.IsSameFaction(owner, currentTurnEntity, ctx)) return;

                if (service.CheckWord(owner, e.Word))
                    onTriggered(new PassiveTriggerContext(owner, null, currentTurnEntity, e.Word));
            });

            var composite = new CompositeDisposable();
            composite.Add(turnSub);
            composite.Add(wordSub);
            composite.Add(new CleanupDisposable(service, owner));
            return composite;
        }

        private sealed class CleanupDisposable : IDisposable
        {
            private readonly ILetterChallengeService _service;
            private readonly EntityId _owner;
            public CleanupDisposable(ILetterChallengeService service, EntityId owner)
            {
                _service = service;
                _owner = owner;
            }
            public void Dispose() => _service.Clear(_owner);
        }
    }
}
