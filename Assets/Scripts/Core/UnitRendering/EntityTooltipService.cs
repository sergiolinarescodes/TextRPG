using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Consumable;
using TextRPG.Core.Encounter;
using TextRPG.Core.Equipment;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Passive;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Systems;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class EntityTooltipService : SystemServiceBase
    {
        private IReadOnlyList<VisualElement> _combatSlotElements;
        private IReadOnlyList<VisualElement> _equipmentSlotElements;
        private IReadOnlyList<VisualElement> _inventorySlotElements;

        private ITooltipService _tooltipService;
        private KeywordSubTooltipProvider _keywordProvider;

        private ICombatSlotService _slotService;
        private IStatusEffectService _statusEffects;
        private IUnitService _unitService;
        private IPassiveService _passiveService;
        private IEncounterService _encounterService;
        private IEventEncounterService _eventEncounterService;
        private IActionRegistry _actionRegistry;
        private IActionHandlerRegistry _handlerRegistry;
        private IWordResolver _enemyResolver;
        private IWordResolver _ammoResolver;
        private IWeaponService _weaponService;
        private IConsumableService _consumableService;
        private IEquipmentService _equipmentService;
        private IItemRegistry _itemRegistry;
        private IEntityStatsService _entityStats;
        private IInventoryService _inventoryService;
        private InventoryId _playerInventoryId;
        private EntityId _playerId;

        private TooltipHandle _currentHandle;
        private readonly List<(VisualElement cell, EventCallback<PointerEnterEvent> enter, EventCallback<PointerLeaveEvent> leave)> _hoverCallbacks = new();

        private static readonly string[] SlotTypeNames = { "Head", "Wear", "Accessory", "Consumable", "Weapon" };

        public EntityTooltipService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<EntityDiedEvent>(_ => HideTooltip());
        }

        public void Initialize(
            IReadOnlyList<VisualElement> combatSlotElements,
            IReadOnlyList<VisualElement> equipmentSlotElements,
            IReadOnlyList<VisualElement> inventorySlotElements,
            ITooltipService tooltipService,
            ICombatSlotService slotService,
            IStatusEffectService statusEffects,
            IUnitService unitService,
            IPassiveService passiveService,
            IEncounterService encounterService,
            IActionRegistry actionRegistry,
            IActionHandlerRegistry handlerRegistry,
            IWordResolver enemyResolver,
            IWordResolver ammoResolver,
            IWeaponService weaponService,
            IConsumableService consumableService,
            IEquipmentService equipmentService,
            IItemRegistry itemRegistry,
            IEntityStatsService entityStats,
            IInventoryService inventoryService,
            InventoryId playerInventoryId,
            EntityId playerId,
            IEventEncounterService eventEncounterService = null)
        {
            _combatSlotElements = combatSlotElements;
            _equipmentSlotElements = equipmentSlotElements;
            _inventorySlotElements = inventorySlotElements;
            _tooltipService = tooltipService;
            _handlerRegistry = handlerRegistry;
            _keywordProvider = new KeywordSubTooltipProvider(actionRegistry, handlerRegistry);
            _slotService = slotService;
            _statusEffects = statusEffects;
            _unitService = unitService;
            _passiveService = passiveService;
            _encounterService = encounterService;
            _actionRegistry = actionRegistry;
            _enemyResolver = enemyResolver;
            _ammoResolver = ammoResolver;
            _weaponService = weaponService;
            _consumableService = consumableService;
            _equipmentService = equipmentService;
            _itemRegistry = itemRegistry;
            _entityStats = entityStats;
            _inventoryService = inventoryService;
            _playerInventoryId = playerInventoryId;
            _playerId = playerId;
            _eventEncounterService = eventEncounterService;

            RegisterHoverCallbacks();
        }

        public void SetEventEncounterService(IEventEncounterService service)
        {
            _eventEncounterService = service;
        }

        private void RegisterHoverCallbacks()
        {
            for (int i = 0; i < _combatSlotElements.Count; i++)
            {
                var cell = _combatSlotElements[i];
                var idx = i;
                RegisterHover(cell, () => OnCombatSlotEnter(idx, cell));
            }
            for (int i = 0; i < _equipmentSlotElements.Count; i++)
            {
                var cell = _equipmentSlotElements[i];
                var idx = i;
                RegisterHover(cell, () => OnEquipmentSlotEnter(idx, cell));
            }
            for (int i = 0; i < _inventorySlotElements.Count; i++)
            {
                var cell = _inventorySlotElements[i];
                var idx = i;
                RegisterHover(cell, () => OnInventorySlotEnter(idx, cell));
            }
        }

        private void RegisterHover(VisualElement cell, Action onEnter)
        {
            EventCallback<PointerEnterEvent> enter = _ => onEnter();
            EventCallback<PointerLeaveEvent> leave = _ => HideTooltip();
            cell.RegisterCallback(enter);
            cell.RegisterCallback(leave);
            _hoverCallbacks.Add((cell, enter, leave));
        }

        private void OnCombatSlotEnter(int cellIndex, VisualElement cell)
        {
            HideTooltip();

            EntityId? entityId = cellIndex < 3
                ? _slotService.GetEntityAt(SlotType.Enemy, cellIndex)
                : _slotService.GetEntityAt(SlotType.Ally, cellIndex - 3);

            if (!entityId.HasValue) return;

            // Resolve entity definition from whichever service owns it
            EntityDefinition entityDef = null;
            InteractableDefinition interactableDef = null;

            if (_encounterService != null && _encounterService.IsEnemy(entityId.Value))
            {
                entityDef = _encounterService.GetEntityDefinition(entityId.Value);
            }
            else if (_eventEncounterService != null && _eventEncounterService.IsEncounterActive)
            {
                try
                {
                    entityDef = _eventEncounterService.GetEntityDefinition(entityId.Value);
                    interactableDef = _eventEncounterService.GetDefinition(entityId.Value);
                }
                catch { /* not an interactable */ }
            }

            // Resolve display name/color from UnitService (works for both enemies and interactables)
            string unitName = entityDef?.Name ?? entityId.Value.Value;
            var unitColor = entityDef?.Color ?? Color.white;
            var unitId = new UnitId(entityId.Value.Value);
            if (_unitService != null && _unitService.TryGetUnit(unitId, out var unitInstance))
            {
                unitName = unitInstance.Definition.Name;
                unitColor = unitInstance.Definition.Color;
            }

            // Collect all ability actions for sub-tooltip detection
            var allActions = new List<WordActionMapping>();
            if (entityDef?.Abilities != null)
            {
                foreach (var word in entityDef.Abilities)
                {
                    var actions = _enemyResolver.Resolve(word);
                    allActions.AddRange(actions);
                }
            }

            var subTooltips = _keywordProvider.DetectKeywords(allActions);

            var capturedEntityId = entityId.Value;
            var capturedUnitName = unitName;
            var capturedUnitColor = unitColor;
            var capturedEntityDef = entityDef;
            var capturedInteractableDef = interactableDef;

            var content = TooltipContent.FromCustom(() =>
            {
                var tooltip = new VisualElement();
                tooltip.pickingMode = PickingMode.Ignore;
                tooltip.Add(TooltipContentBuilder.BuildHeader(capturedUnitName, capturedUnitColor,
                    capturedEntityDef?.Description));
                TooltipContentBuilder.AddStatsRow(tooltip, _entityStats, capturedEntityId);

                // Tags (works for enemies and interactables)
                TooltipContentBuilder.AddTagsSection(tooltip, capturedEntityDef?.Tags);

                // Abilities (enemies)
                if (capturedEntityDef?.Abilities != null && capturedEntityDef.Abilities.Length > 0)
                    TooltipContentBuilder.AddAbilitiesSection(tooltip, capturedEntityDef.Abilities,
                        _enemyResolver, _actionRegistry, _handlerRegistry);

                TooltipContentBuilder.AddPassivesSection(tooltip, _passiveService?.GetPassives(capturedEntityId));
                TooltipContentBuilder.AddStatusEffectsSection(tooltip, _statusEffects?.GetEffects(capturedEntityId));
                return tooltip;
            });

            if (subTooltips.Count > 0)
                content = content.WithSubTooltips(subTooltips);

            _currentHandle = _tooltipService.Show(content, TooltipAnchor.FromElement(cell),
                TooltipPlacement.Auto, TooltipStyles.EntityTooltip);
        }

        private void OnEquipmentSlotEnter(int slotIndex, VisualElement cell)
        {
            HideTooltip();

            var slotType = (EquipmentSlotType)slotIndex;
            if (!_equipmentService.HasEquipped(_playerId, slotType)) return;

            var item = _equipmentService.GetEquipped(_playerId, slotType);
            if (item == null) return;

            TooltipContent content;
            var allActions = new List<WordActionMapping>();

            if (slotType == EquipmentSlotType.Weapon)
            {
                int dur = _weaponService?.GetCurrentDurability(_playerId) ?? 0;
                var ammoWords = _weaponService?.GetAmmoWords(_playerId);

                // Show only the first (primary) ammo word
                IReadOnlyList<string> displayAmmo = ammoWords != null && ammoWords.Count > 0
                    ? new[] { ammoWords[0] }
                    : ammoWords;

                if (displayAmmo != null)
                    foreach (var w in displayAmmo)
                        allActions.AddRange(_ammoResolver.Resolve(w));

                var capturedItem = item;
                var capturedDur = dur;
                var capturedAmmo = displayAmmo;

                content = TooltipContent.FromCustom(() =>
                    TooltipContentBuilder.BuildEquipmentContent(capturedItem, capturedDur,
                        capturedAmmo, _ammoResolver, _actionRegistry, _handlerRegistry));
            }
            else if (slotType == EquipmentSlotType.Consumable)
            {
                int dur = _consumableService?.GetDurability(_playerId) ?? 0;
                var ammoWords = _consumableService?.GetAmmoWords(_playerId);

                // Show only the first (primary) ammo word
                IReadOnlyList<string> displayAmmo = ammoWords != null && ammoWords.Count > 0
                    ? new[] { ammoWords[0] }
                    : ammoWords;

                if (displayAmmo != null)
                    foreach (var w in displayAmmo)
                        allActions.AddRange(_ammoResolver.Resolve(w));

                var capturedItem = item;
                var capturedDur = dur;
                var capturedAmmo = displayAmmo;

                content = TooltipContent.FromCustom(() =>
                    TooltipContentBuilder.BuildEquipmentContent(capturedItem, capturedDur,
                        capturedAmmo, _ammoResolver, _actionRegistry, _handlerRegistry));
            }
            else
            {
                var capturedItem = item;
                content = TooltipContent.FromCustom(() =>
                    TooltipContentBuilder.BuildArmorContent(capturedItem));
            }

            var subTooltips = _keywordProvider.DetectKeywords(allActions);
            if (subTooltips.Count > 0)
                content = content.WithSubTooltips(subTooltips);

            _currentHandle = _tooltipService.Show(content, TooltipAnchor.FromElement(cell),
                TooltipPlacement.Auto, TooltipStyles.EntityTooltip);
        }

        private void OnInventorySlotEnter(int slotIndex, VisualElement cell)
        {
            HideTooltip();

            if (_inventoryService == null) return;
            var slot = _inventoryService.GetSlot(_playerInventoryId, slotIndex);
            if (slot.IsEmpty) return;

            var itemWord = slot.ItemId.Value;
            if (!_itemRegistry.TryGet(itemWord, out var item)) return;

            var allActions = new List<WordActionMapping>();
            if (item.AmmoWords != null)
                foreach (var w in item.AmmoWords)
                    allActions.AddRange(_ammoResolver.Resolve(w));

            var capturedItem = item;
            var slotTypeName = SlotTypeName((int)item.SlotType);

            TooltipContent content;
            if (item.SlotType == EquipmentSlotType.Weapon || item.SlotType == EquipmentSlotType.Consumable)
            {
                content = TooltipContent.FromCustom(() =>
                {
                    var root = new VisualElement();
                    root.pickingMode = PickingMode.Ignore;

                    var equipLabel = new Label($"Equip: {slotTypeName}");
                    equipLabel.style.color = Color.white;
                    equipLabel.style.fontSize = 22;
                    equipLabel.style.marginBottom = 4;
                    equipLabel.pickingMode = PickingMode.Ignore;
                    root.Add(equipLabel);

                    var inner = TooltipContentBuilder.BuildEquipmentContent(capturedItem, capturedItem.Durability,
                        capturedItem.AmmoWords, _ammoResolver, _actionRegistry, _handlerRegistry);
                    root.Add(inner);
                    return root;
                });
            }
            else
            {
                content = TooltipContent.FromCustom(() =>
                {
                    var root = new VisualElement();
                    root.pickingMode = PickingMode.Ignore;

                    var equipLabel = new Label($"Equip: {slotTypeName}");
                    equipLabel.style.color = Color.white;
                    equipLabel.style.fontSize = 22;
                    equipLabel.style.marginBottom = 4;
                    equipLabel.pickingMode = PickingMode.Ignore;
                    root.Add(equipLabel);

                    var inner = TooltipContentBuilder.BuildArmorContent(capturedItem);
                    root.Add(inner);
                    return root;
                });
            }

            var subTooltips = _keywordProvider.DetectKeywords(allActions);
            if (subTooltips.Count > 0)
                content = content.WithSubTooltips(subTooltips);

            _currentHandle = _tooltipService.Show(content, TooltipAnchor.FromElement(cell),
                TooltipPlacement.Auto, TooltipStyles.EntityTooltip);
        }

        private static string SlotTypeName(int index) =>
            index >= 0 && index < SlotTypeNames.Length ? SlotTypeNames[index] : "";

        private void HideTooltip()
        {
            if (_currentHandle != null)
            {
                _tooltipService?.Hide(_currentHandle);
                _currentHandle = null;
            }
        }

        public override void Dispose()
        {
            HideTooltip();

            foreach (var (cell, enter, leave) in _hoverCallbacks)
            {
                cell.UnregisterCallback(enter);
                cell.UnregisterCallback(leave);
            }
            _hoverCallbacks.Clear();

            _combatSlotElements = null;
            _equipmentSlotElements = null;
            _inventorySlotElements = null;
            _tooltipService = null;
            _keywordProvider = null;
            _slotService = null;
            _statusEffects = null;
            _unitService = null;
            _passiveService = null;
            _encounterService = null;
            _eventEncounterService = null;
            _actionRegistry = null;
            _handlerRegistry = null;
            _enemyResolver = null;
            _ammoResolver = null;
            _weaponService = null;
            _consumableService = null;
            _equipmentService = null;
            _itemRegistry = null;
            _entityStats = null;
            _inventoryService = null;

            base.Dispose();
        }
    }
}
