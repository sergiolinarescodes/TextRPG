using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.Experience;
using TextRPG.Core.Scroll;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;
using Unidad.Core.Systems;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.PlayerClass
{
    internal sealed class ClassService : SystemServiceBase, IClassService, IActionValueModifier
    {
        private static readonly HashSet<string> DamageActions = new(StringComparer.OrdinalIgnoreCase)
        {
            ActionNames.Damage, ActionNames.Smash, ActionNames.MagicDamage, ActionNames.WeaponDamage
        };

        private readonly PlayerClass _selectedClass;
        private readonly ClassDefinition _definition;
        private readonly EntityId _playerId;
        private readonly IWordTagResolver _wordTagResolver;
        private readonly ISpellService _spellService;
        private readonly IWordResolver _wordResolver;
        private readonly IResourceService _resourceService;
        private readonly IExperienceService _experienceService;
        private readonly IInventoryService _inventoryService;
        private readonly InventoryId _playerInventoryId;
        private readonly Random _rng = new();
        private bool _isGrantingBonus;

        public PlayerClass SelectedClass => _selectedClass;
        public ClassDefinition Definition => _definition;

        public ClassService(IEventBus eventBus, PlayerClass selectedClass, EntityId playerId,
            IWordTagResolver wordTagResolver,
            ISpellService spellService = null,
            IWordResolver wordResolver = null,
            IResourceService resourceService = null,
            IExperienceService experienceService = null,
            IInventoryService inventoryService = null,
            InventoryId playerInventoryId = default)
            : base(eventBus)
        {
            _selectedClass = selectedClass;
            _definition = ClassDefinitions.Get(selectedClass);
            _playerId = playerId;
            _wordTagResolver = wordTagResolver;
            _spellService = spellService;
            _wordResolver = wordResolver;
            _resourceService = resourceService;
            _experienceService = experienceService;
            _inventoryService = inventoryService;
            _playerInventoryId = playerInventoryId;

            if (selectedClass == PlayerClass.Mage && _spellService != null && _wordResolver != null
                && _inventoryService != null)
                Subscribe<LevelUpEvent>(OnLevelUp);

            if (selectedClass == PlayerClass.Merchant && _resourceService != null)
                Subscribe<ResourceChangedEvent>(OnResourceChanged);

            if (selectedClass == PlayerClass.Rogue && _experienceService != null)
                Subscribe<ChestOpenedEvent>(OnChestOpened);
        }

        private void OnLevelUp(LevelUpEvent evt)
        {
            int levelsGained = evt.NewLevel - evt.PreviousLevel;
            for (int i = 0; i < levelsGained; i++)
            {
                var exclude = new HashSet<string>(_spellService.OfferedOriginals);
                var scroll = ScrollGenerator.Generate(_wordResolver, exclude, _rng);
                if (scroll == null) break;

                var itemKey = $"scroll_{scroll.ScrambledWord}";
                _spellService.RegisterScrollItem(itemKey, scroll);
                _inventoryService.DefineItem(new ItemDefinition(new ItemId(itemKey), scroll.DisplayName, 1));
                _inventoryService.Add(_playerInventoryId, new ItemId(itemKey));
                Publish(new ScrollAcquiredEvent(itemKey, scroll));
            }
        }

        public int ModifyValue(string actionId, int baseValue, string word, EntityId source)
        {
            if (_selectedClass != PlayerClass.Warrior) return baseValue;
            if (!source.Equals(_playerId)) return baseValue;
            if (!DamageActions.Contains(actionId)) return baseValue;
            if (!_wordTagResolver.HasTag(word, "MELEE")) return baseValue;
            return (int)(baseValue * 1.5f);
        }

        private void OnChestOpened(ChestOpenedEvent evt)
        {
            int bonusXp = 2 + _experienceService.CurrentLevel;
            _experienceService.GrantBonusXp(bonusXp);
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (_isGrantingBonus) return;
            if (evt.Id != ResourceIds.Gold) return;
            if (evt.NewValue <= evt.OldValue) return;

            _isGrantingBonus = true;
            try
            {
                _resourceService.Add(ResourceIds.Gold, 1f);
            }
            finally
            {
                _isGrantingBonus = false;
            }
        }
    }
}
