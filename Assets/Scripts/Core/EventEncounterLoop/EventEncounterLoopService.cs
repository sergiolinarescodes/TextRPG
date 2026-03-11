using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Run;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
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
        private readonly ICombatContext _combatContext;
        private readonly IWordCooldownService _wordCooldown;
        private readonly IGiveValidator _giveValidator;
        private readonly EntityId _playerId;

        private readonly int _maxInteractions;

        private bool _active;
        private bool _waitingForAnimation;
        private int _submitCount;

        public bool IsActive => _active;

        public EventEncounterLoopService(
            IEventBus eventBus,
            IEntityStatsService entityStats,
            IWordResolver wordResolver,
            IEventEncounterService encounterService,
            EntityId playerId,
            IReservedWordHandler reservedWordHandler = null,
            ICombatContext combatContext = null,
            IWordCooldownService wordCooldown = null,
            IGiveValidator giveValidator = null,
            int maxInteractions = 0) : base(eventBus)
        {
            _entityStats = entityStats;
            _wordResolver = wordResolver;
            _encounterService = encounterService;
            _reservedWordHandler = reservedWordHandler;
            _combatContext = combatContext;
            _wordCooldown = wordCooldown;
            _giveValidator = giveValidator;
            _playerId = playerId;
            _maxInteractions = maxInteractions;

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

            var giveRejection = WordPrefixHelper.PreprocessGive(ref word, _giveValidator, out bool isGive);
            if (giveRejection.HasValue) return giveRejection.Value;

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

            if (WordCooldownHelper.TryRejectCooldown(_wordCooldown, word, _submitCount, EventBus))
                return WordSubmitResult.WordOnCooldown;

            _wordCooldown?.MarkWordUsed(word, _submitCount);
            _submitCount++;

            // Set target inversion before action execution
            if (isGive)
            {
                _combatContext?.SetTargetingInverted(true);
                _combatContext?.SetGiveCommand(true);
            }

            _waitingForAnimation = true;
            Publish(new WordSubmittedEvent(word));
            return WordSubmitResult.Accepted;
        }

        private void OnAnimationCompleted()
        {
            if (!_active) return;
            _waitingForAnimation = false;

            if (_maxInteractions > 0 && _submitCount >= _maxInteractions)
            {
                _encounterService.EndEncounter();
            }
        }

        private void OnEncounterEnded()
        {
            _active = false;
            _waitingForAnimation = false;
            Debug.Log("[EventEncounterLoop] Encounter ended, loop stopped");
        }
    }
}
