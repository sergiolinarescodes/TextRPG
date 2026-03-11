using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.Consumable;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Run;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.CombatLoop
{
    internal sealed class CombatLoopService : SystemServiceBase, ICombatLoopService
    {
        private readonly ITurnService _turnService;
        private readonly IEntityStatsService _entityStats;
        private readonly IWordResolver _wordResolver;
        private readonly IWeaponService _weaponService;
        private readonly IConsumableService _consumableService;
        private readonly IReservedWordHandler _reservedWordHandler;
        private readonly ICombatContext _combatContext;
        private readonly IWordCooldownService _wordCooldown;
        private readonly IGiveValidator _giveValidator;
        private readonly EntityId _playerId;

        private bool _isPlayerTurn;
        private bool _gameOver;
        private bool _pendingTurnAdvance;
        private bool _advancing;

        public bool IsPlayerTurn => _isPlayerTurn;
        public bool IsGameOver => _gameOver;

        public CombatLoopService(
            IEventBus eventBus,
            ITurnService turnService,
            IEntityStatsService entityStats,
            IWordResolver wordResolver,
            IWeaponService weaponService,
            EntityId playerId,
            IConsumableService consumableService = null,
            IReservedWordHandler reservedWordHandler = null,
            ICombatContext combatContext = null,
            IWordCooldownService wordCooldown = null,
            IGiveValidator giveValidator = null) : base(eventBus)
        {
            _turnService = turnService;
            _entityStats = entityStats;
            _wordResolver = wordResolver;
            _weaponService = weaponService;
            _consumableService = consumableService;
            _reservedWordHandler = reservedWordHandler;
            _combatContext = combatContext;
            _wordCooldown = wordCooldown;
            _giveValidator = giveValidator;
            _playerId = playerId;

            Subscribe<ActionAnimationCompletedEvent>(_ => OnAnimationCompleted());
            Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        public void Start()
        {
            _turnService.BeginTurn();
            _isPlayerTurn = true;
            _pendingTurnAdvance = false;
            _gameOver = false;
            Publish(new PlayerTurnStartedEvent(_turnService.CurrentTurnNumber, _turnService.CurrentRoundNumber));
        }

        public WordSubmitResult SubmitWord(string word)
        {
            word = word?.Trim()?.ToLowerInvariant() ?? "";
            if (word.Length == 0) return WordSubmitResult.InvalidWord;

            bool isGive = WordPrefixHelper.TryStripGivePrefix(ref word);
            if (isGive && word.Length == 0) return WordSubmitResult.InvalidWord;

            if (isGive && _giveValidator != null && _giveValidator.RequiresItemForGive(word))
            {
                if (!_giveValidator.TryConsumeForGive(word))
                    return WordSubmitResult.NoItemToGive;
            }

            if (_reservedWordHandler?.TryHandleReservedWord(word) == true)
                return WordSubmitResult.ReservedWord;

            if (_gameOver) return WordSubmitResult.GameOver;
            if (!_isPlayerTurn) return WordSubmitResult.NotPlayerTurn;

            bool isWeaponAmmo = !isGive && _weaponService != null && _weaponService.HasWeapon(_playerId)
                          && _weaponService.IsAmmoForEquipped(_playerId, word);
            bool isConsumableAmmo = !isGive && !isWeaponAmmo && _consumableService != null
                          && _consumableService.HasConsumable(_playerId)
                          && _consumableService.IsAmmoForEquipped(_playerId, word);
            bool isAmmo = isWeaponAmmo || isConsumableAmmo;
            bool validAction = isAmmo || _wordResolver.HasWord(word);
            if (!validAction) return WordSubmitResult.InvalidWord;

            if (!isAmmo)
            {
                var meta = _wordResolver.GetStats(word);
                if (meta.Cost > 0 && _entityStats.GetCurrentMana(_playerId) < meta.Cost)
                {
                    Publish(new WordRejectedEvent(word, meta.Cost));
                    return WordSubmitResult.InsufficientMana;
                }
            }

            if (!isAmmo && WordCooldownHelper.TryRejectCooldown(_wordCooldown, word, _turnService.CurrentRoundNumber, EventBus))
                return WordSubmitResult.WordOnCooldown;

            // Advance turns BEFORE publishing the action event so that _pendingTurnAdvance
            // is set before any synchronous animation completion can fire.
            AdvanceTurns();

            // Mark word used for cooldown tracking (ammo bypasses)
            if (!isAmmo)
                _wordCooldown?.MarkWordUsed(word, _turnService.CurrentRoundNumber);

            // Set target inversion before action execution
            if (isGive)
            {
                _combatContext?.SetTargetingInverted(true);
                _combatContext?.SetGiveCommand(true);
            }

            if (isWeaponAmmo)
                Publish(new WeaponAmmoSubmittedEvent(_playerId, word));
            else if (isConsumableAmmo)
                Publish(new ConsumableAmmoSubmittedEvent(_playerId, word));
            else
                Publish(new WordSubmittedEvent(word));

            return WordSubmitResult.Accepted;
        }

        public bool FireWeapon()
        {
            if (!_isPlayerTurn || _gameOver) return false;
            if (_weaponService == null || !_weaponService.HasWeapon(_playerId)) return false;

            var ammoWords = _weaponService.GetAmmoWords(_playerId);
            if (ammoWords.Count == 0) return false;

            var ammo = ammoWords[Random.Range(0, ammoWords.Count)];
            Debug.Log($"[CombatLoop] Fire weapon: {ammo}");

            AdvanceTurns();
            Publish(new WeaponAmmoSubmittedEvent(_playerId, ammo));
            return true;
        }

        public bool UseConsumable()
        {
            if (!_isPlayerTurn || _gameOver) return false;
            if (_consumableService == null || !_consumableService.HasConsumable(_playerId)) return false;

            var ammoWords = _consumableService.GetAmmoWords(_playerId);
            if (ammoWords.Count == 0) return false;

            var ammo = ammoWords[Random.Range(0, ammoWords.Count)];
            Debug.Log($"[CombatLoop] Use consumable: {ammo}");

            AdvanceTurns();
            Publish(new ConsumableAmmoSubmittedEvent(_playerId, ammo));
            return true;
        }

        private void AdvanceTurns()
        {
            if (!_isPlayerTurn || _gameOver) return;

            _isPlayerTurn = false;
            Publish(new PlayerTurnEndedEvent());
            _pendingTurnAdvance = true;
        }

        private void AdvanceToNextTurn()
        {
            if (_gameOver) return;
            if (_advancing) return;
            _advancing = true;

            try
            {
                do
                {
                    _turnService.EndTurn();
                    _pendingTurnAdvance = true;
                    _turnService.BeginTurn();
                    var current = _turnService.CurrentEntity;

                    if (current.Equals(_playerId))
                    {
                        _isPlayerTurn = true;
                        _pendingTurnAdvance = false;
                        Publish(new PlayerTurnStartedEvent(_turnService.CurrentTurnNumber, _turnService.CurrentRoundNumber));
                        return;
                    }

                    if (_entityStats.GetCurrentHealth(current) <= 0)
                    {
                        _pendingTurnAdvance = false;
                        continue;
                    }

                    Debug.Log($"[CombatLoop] {current.Value} turn processing");
                } while (!_pendingTurnAdvance);
            }
            finally
            {
                _advancing = false;
            }
        }

        private void OnAnimationCompleted()
        {
            if (_pendingTurnAdvance)
            {
                _pendingTurnAdvance = false;
                AdvanceToNextTurn();
            }
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (!evt.EntityId.Equals(_playerId)) return;
            _gameOver = true;
            _isPlayerTurn = false;
            Publish(new GameOverEvent(_playerId));
        }
    }
}
