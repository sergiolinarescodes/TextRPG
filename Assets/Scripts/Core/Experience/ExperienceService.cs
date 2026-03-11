using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Experience
{
    internal sealed class ExperienceService : SystemServiceBase, IExperienceService
    {
        private readonly ILootRewardService _lootRewardService;
        private IEncounterService _encounterService;
        private int _currentLevel = 1;
        private int _currentXp;
        private int _pendingLevelUps;

        public int CurrentLevel => _currentLevel;
        public int CurrentXp => _currentXp;
        public int XpForNextLevel => _currentLevel * 20;
        public float XpProgress => XpForNextLevel > 0 ? (float)_currentXp / XpForNextLevel : 0f;

        public ExperienceService(IEventBus eventBus, ILootRewardService lootRewardService) : base(eventBus)
        {
            _lootRewardService = lootRewardService;
            Subscribe<EntityDiedEvent>(OnEntityDied);
            Subscribe<LootRewardSelectedEvent>(OnLootSelected);
        }

        internal void SetEncounterService(IEncounterService encounterService)
        {
            _encounterService = encounterService;
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (_encounterService == null || !_encounterService.IsEnemy(evt.EntityId))
                return;

            var def = _encounterService.GetEntityDefinition(evt.EntityId);
            int xpAmount = def.Tier * 5 + def.MaxHealth / 2;
            _currentXp += xpAmount;

            int previousLevel = _currentLevel;
            while (_currentXp >= XpForNextLevel)
            {
                _currentXp -= XpForNextLevel;
                _currentLevel++;
                _pendingLevelUps++;
            }

            Publish(new ExperienceGainedEvent(evt.EntityId, xpAmount, _currentXp, XpForNextLevel, _currentLevel));

            if (_currentLevel > previousLevel)
                Publish(new LevelUpEvent(_currentLevel, previousLevel));
        }

        private void OnLootSelected(LootRewardSelectedEvent evt)
        {
            if (_pendingLevelUps > 0)
            {
                _pendingLevelUps--;
                _lootRewardService.GenerateAndOffer();
            }
        }
    }
}
