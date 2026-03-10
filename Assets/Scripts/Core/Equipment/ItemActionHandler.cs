using TextRPG.Core.ActionExecution;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Systems;

namespace TextRPG.Core.Equipment
{
    internal sealed class ItemActionHandler : SystemServiceBase, IActionHandler
    {
        private readonly IInventoryService _inventoryService;
        private readonly InventoryId _playerInventoryId;
        private readonly IEquipmentService _equipmentService;
        private readonly IItemRegistry _itemRegistry;
        private string _lastAcquiredWord;

        public string ActionId => ActionNames.Item;

        public ItemActionHandler(IActionHandlerContext ctx, IInventoryService inventoryService, InventoryId playerInventoryId,
            IEquipmentService equipmentService = null, IItemRegistry itemRegistry = null) : base(ctx.EventBus)
        {
            _inventoryService = inventoryService;
            _playerInventoryId = playerInventoryId;
            _equipmentService = equipmentService;
            _itemRegistry = itemRegistry;

            Subscribe<ActionExecutionCompletedEvent>(_ => _lastAcquiredWord = null);
        }

        public void Execute(ActionContext context)
        {
            var itemWord = context.Word;

            // Item words with multiple ammo rows call Execute per row — only acquire once
            if (itemWord == _lastAcquiredWord) return;
            _lastAcquiredWord = itemWord;

            var itemId = new ItemId(itemWord);

            // Auto-equip weapon and consumable-type items directly
            if (_equipmentService != null && _itemRegistry != null
                && _itemRegistry.TryGet(itemWord, out var itemDef)
                && (itemDef.SlotType == EquipmentSlotType.Weapon || itemDef.SlotType == EquipmentSlotType.Consumable))
            {
                _equipmentService.Equip(context.Source, itemWord);
                Publish(new ItemAcquiredEvent(context.Source, itemWord));
                return;
            }

            if (_inventoryService != null && _inventoryService.HasItemDefinition(itemId))
            {
                var overflow = _inventoryService.Add(_playerInventoryId, itemId);
                if (overflow > 0)
                    UnityEngine.Debug.Log($"[Equipment] Inventory full, could not add {itemWord}");
            }

            Publish(new ItemAcquiredEvent(context.Source, itemWord));
        }
    }
}
