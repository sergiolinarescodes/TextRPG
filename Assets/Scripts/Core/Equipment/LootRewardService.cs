using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
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
        private EquipmentItemDefinition[] _pendingOptions;

        public bool IsAwaitingSelection => _pendingOptions != null;

        public LootRewardService(
            IEventBus eventBus,
            IItemRegistry itemRegistry,
            IInventoryService inventoryService,
            InventoryId playerInventoryId,
            EntityId playerId) : base(eventBus)
        {
            _itemRegistry = itemRegistry;
            _inventoryService = inventoryService;
            _playerInventoryId = playerInventoryId;
            _playerId = playerId;

            Subscribe<EncounterEndedEvent>(OnEncounterEnded);
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            if (evt.Victory)
                GenerateAndOffer();
        }

        public void GenerateAndOffer()
        {
            var options = BasicEquipmentGenerator.GenerateRewards();

            foreach (var item in options)
            {
                _itemRegistry.Register(item.ItemWord, item);
                _inventoryService.DefineItem(new ItemDefinition(new ItemId(item.ItemWord), item.DisplayName, 1));
            }

            _pendingOptions = options;
            Publish(new LootRewardOfferedEvent(options));
        }

        public void SelectReward(int index)
        {
            if (_pendingOptions == null || index < 0 || index >= _pendingOptions.Length) return;

            var selected = _pendingOptions[index];
            var itemId = new ItemId(selected.ItemWord);

            var overflow = _inventoryService.Add(_playerInventoryId, itemId);
            if (overflow > 0)
                Debug.LogWarning($"[LootReward] Inventory full, could not add {selected.DisplayName}");

            Publish(new ItemAcquiredEvent(_playerId, selected.ItemWord));

            _pendingOptions = null;
            Publish(new LootRewardSelectedEvent(selected));
        }
    }
}
