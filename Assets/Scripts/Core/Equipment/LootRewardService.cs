using System;
using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Scroll;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Equipment
{
    internal sealed class LootRewardService : SystemServiceBase, ILootRewardService
    {
        private readonly IItemRegistry _itemRegistry;
        private readonly IInventoryService _inventoryService;
        private readonly InventoryId _playerInventoryId;
        private readonly EntityId _playerId;
        private readonly ISpellService _spellService;
        private readonly IWordResolver _baseResolver;
        private readonly System.Random _rng = new();
        private LootRewardOption[] _pendingOptions;

        public bool IsAwaitingSelection => _pendingOptions != null;

        public LootRewardService(
            IEventBus eventBus,
            IItemRegistry itemRegistry,
            IInventoryService inventoryService,
            InventoryId playerInventoryId,
            EntityId playerId,
            ISpellService spellService = null,
            IWordResolver baseResolver = null) : base(eventBus)
        {
            _itemRegistry = itemRegistry;
            _inventoryService = inventoryService;
            _playerInventoryId = playerInventoryId;
            _playerId = playerId;
            _spellService = spellService;
            _baseResolver = baseResolver;

            Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            Subscribe<RewardGrantedEvent>(OnRewardGranted);
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (evt.Victory)
                GenerateAndOffer();
        }

        private void OnRewardGranted(RewardGrantedEvent evt)
        {
            if (evt.RewardType == "random" && !IsAwaitingSelection)
                GenerateAndOffer();
        }

        public void GenerateAndOffer()
        {
            // Decide if one slot should be a scroll (~33% chance, only if spell service available)
            ScrollDefinition scroll = null;
            if (_spellService != null && _baseResolver != null && _rng.Next(3) == 0)
            {
                var excludes = _spellService.OfferedOriginals as HashSet<string> ?? new HashSet<string>(_spellService.OfferedOriginals);
                scroll = ScrollGenerator.Generate(_baseResolver, excludes, _rng);
            }

            int equipCount = scroll != null ? 2 : 3;
            var equipment = BasicEquipmentGenerator.GenerateRewards(_rng, equipCount);

            foreach (var item in equipment)
            {
                _itemRegistry.Register(item.ItemWord, item);
                _inventoryService.DefineItem(new ItemDefinition(new ItemId(item.ItemWord), item.DisplayName, 1));
            }

            var options = new List<LootRewardOption>();
            foreach (var item in equipment)
                options.Add(new LootRewardOption(item, null));
            if (scroll != null)
                options.Add(new LootRewardOption(null, scroll));

            _pendingOptions = options.ToArray();
            Publish(new LootRewardOfferedEvent(_pendingOptions));
        }

        public void SelectReward(int index)
        {
            if (_pendingOptions == null || index < 0 || index >= _pendingOptions.Length) return;

            var option = _pendingOptions[index];

            if (option.IsScroll)
            {
                var scroll = option.Scroll;
                var itemKey = $"scroll_{scroll.ScrambledWord}";
                _spellService?.RegisterScrollItem(itemKey, scroll);
                _inventoryService.DefineItem(new ItemDefinition(new ItemId(itemKey), scroll.DisplayName, 1));
                _inventoryService.Add(_playerInventoryId, new ItemId(itemKey));
                Publish(new ScrollAcquiredEvent(itemKey, scroll));
            }
            else
            {
                var selected = option.Equipment;
                var itemId = new ItemId(selected.ItemWord);

                var overflow = _inventoryService.Add(_playerInventoryId, itemId);
                if (overflow > 0)
                    Debug.LogWarning($"[LootReward] Inventory full, could not add {selected.DisplayName}");

                Publish(new ItemAcquiredEvent(_playerId, selected.ItemWord));
            }

            _pendingOptions = null;
            Publish(new LootRewardSelectedEvent(option));
        }
    }
}
