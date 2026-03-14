using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Experience
{
    internal sealed class ExperienceService : SystemServiceBase, IExperienceService
    {
        private IEncounterService _encounterService;
        private int _currentLevel = 1;
        private int _currentXp;

        public int CurrentLevel => _currentLevel;
        public int CurrentXp => _currentXp;
        public int XpForNextLevel => _currentLevel * 20;
        public float XpProgress => XpForNextLevel > 0 ? (float)_currentXp / XpForNextLevel : 0f;

        public ExperienceService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<EntityDiedEvent>(OnEntityDied);
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
            }

            Publish(new ExperienceGainedEvent(evt.EntityId, xpAmount, _currentXp, XpForNextLevel, _currentLevel));

            if (_currentLevel > previousLevel)
                Publish(new LevelUpEvent(_currentLevel, previousLevel));
        }

        public void GrantBonusXp(int amount)
        {
            if (amount <= 0) return;
            _currentXp += amount;

            int previousLevel = _currentLevel;
            while (_currentXp >= XpForNextLevel)
            {
                _currentXp -= XpForNextLevel;
                _currentLevel++;
            }

            Publish(new ExperienceGainedEvent(default, amount, _currentXp, XpForNextLevel, _currentLevel));

            if (_currentLevel > previousLevel)
                Publish(new LevelUpEvent(_currentLevel, previousLevel));
        }

    }
}
