using System;
using System.Collections.Generic;
using TextRPG.Core.Consumable;
using TextRPG.Core.Equipment;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class EquipmentVisualController : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IEquipmentService _equipmentService;
        private readonly IInventoryService _inventoryService;
        private readonly IItemRegistry _itemRegistry;
        private readonly IWeaponService _weaponService;
        private readonly IConsumableService _consumableService;
        private readonly InventoryId _playerInventoryId;
        private readonly EntityId _playerId;
        private readonly VisualElement _rootElement;
        private readonly List<IDisposable> _subscriptions = new();

        private VisualElement _weaponSlot;
        private Label _weaponDurabilityLabel;
        private VisualElement _consumableSlot;
        private Label _consumableDurabilityLabel;

        // Drag state
        private VisualElement _dragElement;
        private string _dragItemWord;
        private int _dragSourceSlot = -1;
        private bool _dragFromEquipment;

        public EquipmentBarVisual LeftBar { get; private set; }
        public EquipmentBarVisual RightBar { get; private set; }

        public EquipmentVisualController(IEventBus eventBus, IEquipmentService equipmentService,
            IInventoryService inventoryService, IItemRegistry itemRegistry,
            IWeaponService weaponService, IConsumableService consumableService,
            InventoryId playerInventoryId, EntityId playerId,
            VisualElement rootElement)
        {
            _eventBus = eventBus;
            _equipmentService = equipmentService;
            _inventoryService = inventoryService;
            _itemRegistry = itemRegistry;
            _weaponService = weaponService;
            _consumableService = consumableService;
            _playerInventoryId = playerInventoryId;
            _playerId = playerId;
            _rootElement = rootElement;
        }

        public void BuildBars(VisualElement middleArea)
        {
            LeftBar = new EquipmentBarVisual(EquipmentConstants.InventorySlotCount);
            LeftBar.BuildColumn(middleArea);

            // Build right bar column now (slots must exist before configuration).
            // Caller inserts the main text panel at index 1 between left and right.
            RightBar = new EquipmentBarVisual(EquipmentConstants.SlotCount);
            RightBar.BuildColumn(middleArea);

            RightBar.SetSlotBackground(0, "HEAD", SlotColors.Placeholder);
            RightBar.SetSlotBackground(1, "WEAR", SlotColors.Placeholder);
            RightBar.SetSlotBackground(2, "ACCESSORY", SlotColors.Placeholder);
            RightBar.SetSlotBackground(3, "USE", SlotColors.Placeholder);
            RightBar.SetSlotBackground(4, "WEAPON", SlotColors.Placeholder);

            for (int i = 0; i < EquipmentConstants.InventorySlotCount; i++)
                LeftBar.SetSlotBackground(i, "INVENTORY", SlotColors.Placeholder);

            SetupWeaponSlot();
            SetupConsumableSlot();
            SubscribeToEvents();
            SetupDragAndDrop();
        }

        public Action FireWeaponAction { get; set; }
        public Action UseConsumableAction { get; set; }

        private void SetupWeaponSlot()
        {
            _weaponSlot = RightBar.GetSlotElement(4);
            _weaponSlot.pickingMode = PickingMode.Position;

            _weaponDurabilityLabel = new Label();
            _weaponDurabilityLabel.style.color = new Color(1f, 0.8f, 0.2f);
            _weaponDurabilityLabel.style.fontSize = 20;
            _weaponDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _weaponDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _weaponDurabilityLabel.style.position = Position.Absolute;
            _weaponDurabilityLabel.style.bottom = 2;
            _weaponDurabilityLabel.style.left = 4;

            _weaponSlot.RegisterCallback<ClickEvent>(_ => FireWeaponAction?.Invoke());
        }

        private void SetupConsumableSlot()
        {
            _consumableSlot = RightBar.GetSlotElement(3);
            _consumableSlot.pickingMode = PickingMode.Position;

            _consumableDurabilityLabel = new Label();
            _consumableDurabilityLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _consumableDurabilityLabel.style.fontSize = 20;
            _consumableDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _consumableDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _consumableDurabilityLabel.style.position = Position.Absolute;
            _consumableDurabilityLabel.style.bottom = 2;
            _consumableDurabilityLabel.style.left = 4;

            _consumableSlot.RegisterCallback<ClickEvent>(_ => UseConsumableAction?.Invoke());
        }

        private void SubscribeToEvents()
        {
            _subscriptions.Add(_eventBus.Subscribe<WeaponEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                RightBar?.SetSlotContent(4, evt.Weapon.DisplayName, Color.white);
                _weaponSlot?.Add(_weaponDurabilityLabel);
                _weaponDurabilityLabel.text = evt.Weapon.Durability.ToString();
            }));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDurabilityChangedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                _weaponDurabilityLabel.text = evt.CurrentDurability.ToString();
            }));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDestroyedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                RightBar?.ClearSlotContent(4);
                if (_weaponDurabilityLabel?.parent != null)
                    _weaponDurabilityLabel.RemoveFromHierarchy();
            }));

            _subscriptions.Add(_eventBus.Subscribe<ConsumableEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                RightBar?.SetSlotContent(3, evt.Consumable.DisplayName, new Color(1f, 0.85f, 0.2f));
                _consumableDurabilityLabel.text = evt.Consumable.Durability.ToString();
                _consumableSlot?.Add(_consumableDurabilityLabel);
            }));
            _subscriptions.Add(_eventBus.Subscribe<ConsumableDurabilityChangedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                _consumableDurabilityLabel.text = evt.CurrentDurability.ToString();
            }));
            _subscriptions.Add(_eventBus.Subscribe<ConsumableDestroyedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                RightBar?.ClearSlotContent(3);
                if (_consumableDurabilityLabel?.parent != null)
                    _consumableDurabilityLabel.RemoveFromHierarchy();
            }));

            _subscriptions.Add(_eventBus.Subscribe<SlotChangedEvent>(evt =>
            {
                if (evt.InventoryId != _playerInventoryId) return;
                if (evt.NewSlot.IsEmpty)
                {
                    LeftBar?.ClearSlotContent(evt.SlotIndex);
                }
                else
                {
                    var itemWord = evt.NewSlot.ItemId.Value;
                    if (_itemRegistry.TryGet(itemWord, out var itemDef))
                        LeftBar?.SetSlotContent(evt.SlotIndex, itemDef.DisplayName, itemDef.Color);
                    else
                        LeftBar?.SetSlotContent(evt.SlotIndex, itemWord.ToUpperInvariant(), Color.white);
                }
            }));
            _subscriptions.Add(_eventBus.Subscribe<ItemEquippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                int slotIndex = (int)evt.Slot;
                RightBar?.SetSlotContent(slotIndex, evt.Item.DisplayName, evt.Item.Color);
            }));
            _subscriptions.Add(_eventBus.Subscribe<ItemUnequippedEvent>(evt =>
            {
                if (!evt.Entity.Equals(_playerId)) return;
                int slotIndex = (int)evt.Slot;
                RightBar?.ClearSlotContent(slotIndex);
            }));
        }

        private void SetupDragAndDrop()
        {
            RegisterSlotDragHandlers(LeftBar, EquipmentConstants.InventorySlotCount, OnInventorySlotPointerDown);
            RegisterSlotDragHandlers(RightBar, EquipmentConstants.SlotCount, OnEquipmentSlotPointerDown);

            _rootElement.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_dragElement == null) return;
                UpdateDragPosition(evt.position);
            });
            _rootElement.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_dragElement == null || _dragItemWord == null) return;

                bool handled;
                if (_dragFromEquipment)
                    handled = _equipmentService.UnequipToInventory(
                        _playerId, (EquipmentSlotType)_dragSourceSlot, _inventoryService, _playerInventoryId);
                else
                    handled = TryDropToEquipmentSlot(evt.position);

                CancelDrag();
            });
        }

        private static void RegisterSlotDragHandlers(EquipmentBarVisual bar, int count, Action<PointerDownEvent, int> handler)
        {
            for (int i = 0; i < count; i++)
            {
                var slotIndex = i;
                var slotElement = bar.GetSlotElement(i);
                slotElement.pickingMode = PickingMode.Position;
                slotElement.RegisterCallback<PointerDownEvent>(evt => handler(evt, slotIndex));
            }
        }

        private void OnInventorySlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (_inventoryService == null) return;
            var slot = _inventoryService.GetSlot(_playerInventoryId, slotIndex);
            if (slot.IsEmpty) return;

            _dragItemWord = slot.ItemId.Value;
            _dragSourceSlot = slotIndex;
            _dragFromEquipment = false;
            StartDrag(evt.position);
            evt.StopPropagation();
        }

        private void OnEquipmentSlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (!_equipmentService.CanUnequipInBattle()) return;
            var slotType = (EquipmentSlotType)slotIndex;
            var equipped = _equipmentService.GetEquipped(_playerId, slotType);
            if (equipped == null) return;

            _dragItemWord = equipped.ItemWord;
            _dragSourceSlot = slotIndex;
            _dragFromEquipment = true;
            StartDrag(evt.position);
            evt.StopPropagation();
        }

        private void StartDrag(Vector2 position)
        {
            if (_dragElement != null) return;

            _dragElement = new VisualElement();
            _dragElement.style.position = Position.Absolute;
            _dragElement.style.width = 100;
            _dragElement.style.height = 80;
            _dragElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.85f);
            _dragElement.style.borderTopWidth = 2;
            _dragElement.style.borderBottomWidth = 2;
            _dragElement.style.borderLeftWidth = 2;
            _dragElement.style.borderRightWidth = 2;
            _dragElement.style.borderTopColor = Color.yellow;
            _dragElement.style.borderBottomColor = Color.yellow;
            _dragElement.style.borderLeftColor = Color.yellow;
            _dragElement.style.borderRightColor = Color.yellow;
            _dragElement.style.justifyContent = Justify.Center;
            _dragElement.style.alignItems = Align.Center;
            _dragElement.pickingMode = PickingMode.Ignore;

            var label = new Label(_dragItemWord.ToUpperInvariant());
            label.style.color = Color.white;
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore;
            _dragElement.Add(label);

            _rootElement.Add(_dragElement);
            UpdateDragPosition(position);
        }

        private void UpdateDragPosition(Vector2 position)
        {
            if (_dragElement == null) return;
            _dragElement.style.left = position.x - 50;
            _dragElement.style.top = position.y - 40;
        }

        private bool TryDropToEquipmentSlot(Vector2 dropPos)
        {
            for (int i = 0; i < EquipmentConstants.SlotCount; i++)
            {
                var slotElement = RightBar.GetSlotElement(i);
                if (slotElement == null || !slotElement.worldBound.Contains(dropPos)) continue;

                var targetSlotType = (EquipmentSlotType)i;
                var itemSlotType = _equipmentService.GetSlotTypeForItem(_dragItemWord);
                if (itemSlotType == null || itemSlotType.Value != targetSlotType) return false;
                if (!_equipmentService.CanEquipSlotInBattle(targetSlotType)) return false;

                return _equipmentService.EquipFromInventory(_playerId, _dragItemWord, _inventoryService, _playerInventoryId);
            }
            return false;
        }

        private void CancelDrag()
        {
            _dragElement?.RemoveFromHierarchy();
            _dragElement = null;
            _dragItemWord = null;
            _dragSourceSlot = -1;
            _dragFromEquipment = false;
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            CancelDrag();
            LeftBar = null;
            RightBar = null;
            _weaponSlot = null;
            _weaponDurabilityLabel = null;
            _consumableSlot = null;
            _consumableDurabilityLabel = null;
        }
    }
}
