using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Run;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounterLoop
{
    internal sealed class EventEncounterLoopService : SystemServiceBase, IEventEncounterLoopService
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IWordResolver _wordResolver;
        private readonly IEventEncounterService _encounterService;
        private readonly IReservedWordHandler _reservedWordHandler;
        private readonly EntityId _playerId;

        private bool _active;
        private bool _waitingForAnimation;

        public bool IsActive => _active;

        public EventEncounterLoopService(
            IEventBus eventBus,
            IEntityStatsService entityStats,
            IWordResolver wordResolver,
            IEventEncounterService encounterService,
            EntityId playerId,
            IReservedWordHandler reservedWordHandler = null) : base(eventBus)
        {
            _entityStats = entityStats;
            _wordResolver = wordResolver;
            _encounterService = encounterService;
            _reservedWordHandler = reservedWordHandler;
            _playerId = playerId;

            Subscribe<ActionAnimationCompletedEvent>(_ => OnAnimationCompleted());
            Subscribe<EventEncounterEndedEvent>(_ => OnEncounterEnded());
        }

        public void Start()
        {
            _active = true;
            _waitingForAnimation = false;
            Debug.Log("[EventEncounterLoop] Started");
        }

        public WordSubmitResult SubmitWord(string word)
        {
            word = word?.Trim()?.ToLowerInvariant() ?? "";
            if (word.Length == 0) return WordSubmitResult.InvalidWord;

            if (_reservedWordHandler?.TryHandleReservedWord(word) == true)
                return WordSubmitResult.ReservedWord;

            if (!_active) return WordSubmitResult.GameOver;
            if (_waitingForAnimation) return WordSubmitResult.NotPlayerTurn;

            if (!_wordResolver.HasWord(word)) return WordSubmitResult.InvalidWord;

            var meta = _wordResolver.GetStats(word);
            if (meta.Cost > 0 && _entityStats.GetCurrentMana(_playerId) < meta.Cost)
            {
                Publish(new WordRejectedEvent(word, meta.Cost));
                return WordSubmitResult.InsufficientMana;
            }

            _waitingForAnimation = true;
            Publish(new WordSubmittedEvent(word));
            return WordSubmitResult.Accepted;
        }

        private void OnAnimationCompleted()
        {
            if (!_active) return;
            _waitingForAnimation = false;
        }

        private void OnEncounterEnded()
        {
            _active = false;
            _waitingForAnimation = false;
            Debug.Log("[EventEncounterLoop] Encounter ended, loop stopped");
        }
    }
}
