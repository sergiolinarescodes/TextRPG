using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Equipment.Scenarios
{
    internal sealed class EquipmentScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private IPassiveService _passiveService;
        private IEquipmentService _equipmentService;
        private IInventoryService _inventoryService;
        private EntityId _player;
        private InventoryId _inventoryId;
        private readonly List<IDisposable> _subscriptions = new();

        // Verification results
        private bool _ringEquipStrength;
        private bool _ringEquipLuck;
        private bool _ringUnequipStrength;
        private bool _ringUnequipLuck;
        private bool _helmEquipPhysDef;
        private bool _helmPassivesRegistered;
        private bool _helmUnequipPassivesRemoved;
        private bool _helmUnequipBasePassivesSurvive;
        private bool _equipFromInventoryRemoved;
        private bool _equipFromInventoryEquipped;
        private bool _unequipToInventoryReturned;
        private bool _unequipToInventoryCleared;
        private bool _wrongSlotRejected;
        private bool _deadEntityCleanedUp;

        public EquipmentScenario() : base(new TestScenarioDefinition(
            "equipment-flow",
            "Equipment Flow",
            "Tests equip/unequip stat modifiers, passives, inventory integration, and dead entity cleanup.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            var slotService = new CombatSlotService(_eventBus);
            slotService.Initialize();

            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry();
            var targetResolver = new PassiveTargetResolver();
            var passiveContext = new PassiveContext(_entityStats, slotService, _eventBus, null);
            _passiveService = new PassiveService(_eventBus, triggerRegistry, effectRegistry, targetResolver, passiveContext);

            var weaponRegistry = new WeaponRegistry();
            var weaponService = new WeaponService(_eventBus, weaponRegistry);

            // Build test item registry
            var itemRegistry = new ItemRegistry();
            itemRegistry.Register("test_helm", new EquipmentItemDefinition(
                "test_helm", "Test Helm", EquipmentSlotType.Head, 0,
                new StatBonus(0, 0, 2, 0, 0, 0, 0), Color.gray, Array.Empty<string>(),
                new[] { new PassiveEntry("on_self_hit", null, "shield", null, 1, "Self") }));
            itemRegistry.Register("test_ring", new EquipmentItemDefinition(
                "test_ring", "Test Ring", EquipmentSlotType.Accessory, 0,
                new StatBonus(1, 0, 0, 0, 1, 0, 0), Color.cyan, Array.Empty<string>(),
                Array.Empty<PassiveEntry>()));
            itemRegistry.Register("test_sword", new EquipmentItemDefinition(
                "test_sword", "Test Sword", EquipmentSlotType.Weapon, 3,
                new StatBonus(0, 0, 0, 0, 0, 0, 0), Color.white, new[] { "slash" },
                Array.Empty<PassiveEntry>()));

            _equipmentService = new EquipmentService(_eventBus, itemRegistry, _entityStats, _passiveService, weaponService);

            // Register player entity: HP=50, Str=5, Magic=3, PhysDef=2, MagDef=1, Luck=3
            _player = new EntityId("player");
            _entityStats.RegisterEntity(_player, 50, 5, 3, 2, 1, 3);
            slotService.RegisterAlly(_player, 0);

            // Register base passives for player (to verify unequip doesn't nuke them)
            var basePassives = new[] { new PassiveEntry("on_round_start", null, "heal", null, 1, "Self") };
            _passiveService.RegisterPassives(_player, "unit", basePassives);

            // Set up inventory
            _inventoryService = new InventoryService(_eventBus);
            _inventoryId = new InventoryId("player_inv");
            _inventoryService.Create(_inventoryId, new InventoryDefinition(EquipmentConstants.InventorySlotCount));
            _inventoryService.DefineItem(new ItemDefinition(new ItemId("test_helm"), "Test Helm", 1));
            _inventoryService.DefineItem(new ItemDefinition(new ItemId("test_ring"), "Test Ring", 1));
            _inventoryService.DefineItem(new ItemDefinition(new ItemId("test_sword"), "Test Sword", 1));
            _inventoryService.Add(_inventoryId, new ItemId("test_helm"));
            _inventoryService.Add(_inventoryId, new ItemId("test_ring"));
            _inventoryService.Add(_inventoryId, new ItemId("test_sword"));

            _subscriptions.Add(_eventBus.Subscribe<ItemEquippedEvent>(evt =>
                Debug.Log($"[EquipmentScenario] Equipped: {evt.Item.ItemWord} → {evt.Slot}")));
            _subscriptions.Add(_eventBus.Subscribe<ItemUnequippedEvent>(evt =>
                Debug.Log($"[EquipmentScenario] Unequipped: {evt.Item.ItemWord} from {evt.Slot}")));

            // --- Verification logic ---

            // 1. Equip ring → +1 Str, +1 Luck
            var baseStr = _entityStats.GetStat(_player, StatType.Strength);
            var baseLuck = _entityStats.GetStat(_player, StatType.Luck);
            _equipmentService.Equip(_player, "test_ring");
            _ringEquipStrength = _entityStats.GetStat(_player, StatType.Strength) == baseStr + 1;
            _ringEquipLuck = _entityStats.GetStat(_player, StatType.Luck) == baseLuck + 1;

            // 2. Unequip ring → stats return to base
            _equipmentService.Unequip(_player, EquipmentSlotType.Accessory);
            _ringUnequipStrength = _entityStats.GetStat(_player, StatType.Strength) == baseStr;
            _ringUnequipLuck = _entityStats.GetStat(_player, StatType.Luck) == baseLuck;

            // 3. Equip helm → +2 PhysDef + passives registered
            var basePhysDef = _entityStats.GetStat(_player, StatType.PhysicalDefense);
            _equipmentService.Equip(_player, "test_helm");
            _helmEquipPhysDef = _entityStats.GetStat(_player, StatType.PhysicalDefense) == basePhysDef + 2;

            var helmPassives = _passiveService.GetPassives(_player);
            _helmPassivesRegistered = false;
            for (int i = 0; i < helmPassives.Count; i++)
            {
                if (helmPassives[i].TriggerId == "on_self_hit" && helmPassives[i].EffectId == "shield")
                {
                    _helmPassivesRegistered = true;
                    break;
                }
            }

            // 4. Unequip helm → equip passives removed, but base "unit" passives survive
            _equipmentService.Unequip(_player, EquipmentSlotType.Head);
            _helmUnequipBasePassivesSurvive = _passiveService.HasPassives(_player);
            var remainingPassives = _passiveService.GetPassives(_player);
            _helmUnequipPassivesRemoved = true;
            for (int i = 0; i < remainingPassives.Count; i++)
            {
                if (remainingPassives[i].TriggerId == "on_self_hit" && remainingPassives[i].EffectId == "shield")
                {
                    _helmUnequipPassivesRemoved = false;
                    break;
                }
            }

            // 5. EquipFromInventory → item removed from inventory, equipped
            _equipmentService.EquipFromInventory(_player, "test_ring", _inventoryService, _inventoryId);
            _equipFromInventoryRemoved = !_inventoryService.Contains(_inventoryId, new ItemId("test_ring"));
            _equipFromInventoryEquipped = _equipmentService.HasEquipped(_player, EquipmentSlotType.Accessory);

            // 6. UnequipToInventory → item returned to inventory, slot cleared
            _equipmentService.UnequipToInventory(_player, EquipmentSlotType.Accessory, _inventoryService, _inventoryId);
            _unequipToInventoryReturned = _inventoryService.Contains(_inventoryId, new ItemId("test_ring"));
            _unequipToInventoryCleared = !_equipmentService.HasEquipped(_player, EquipmentSlotType.Accessory);

            // 7. Wrong slot type → equip helm into accessory should fail (helm is Head type, slot validates internally)
            _equipmentService.Equip(_player, "test_helm");
            _wrongSlotRejected = !_equipmentService.Equip(_player, "test_helm"); // already occupied = false
            _equipmentService.Unequip(_player, EquipmentSlotType.Head);

            // 8. Dead entity cleanup → fire EntityDiedEvent, verify _equipped cleared
            _equipmentService.Equip(_player, "test_ring");
            _eventBus.Publish(new EntityDiedEvent(_player));
            _deadEntityCleanedUp = !_equipmentService.HasEquipped(_player, EquipmentSlotType.Accessory);

            // Visual
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = Color.black;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            var title = new Label("Equipment Scenario");
            title.style.color = Color.white;
            title.style.fontSize = 24;
            root.Add(title);

            var info = new Label($"Player: Str={_entityStats.GetStat(_player, StatType.Strength)} " +
                                 $"Luck={_entityStats.GetStat(_player, StatType.Luck)} " +
                                 $"PhysDef={_entityStats.GetStat(_player, StatType.PhysicalDefense)}");
            info.style.color = Color.cyan;
            info.style.fontSize = 18;
            root.Add(info);

            Debug.Log("[EquipmentScenario] Setup complete.");
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Ring equip: Strength +1", _ringEquipStrength,
                    _ringEquipStrength ? null : "Strength did not increase by 1"),
                new("Ring equip: Luck +1", _ringEquipLuck,
                    _ringEquipLuck ? null : "Luck did not increase by 1"),
                new("Ring unequip: Strength restored", _ringUnequipStrength,
                    _ringUnequipStrength ? null : "Strength not restored to base"),
                new("Ring unequip: Luck restored", _ringUnequipLuck,
                    _ringUnequipLuck ? null : "Luck not restored to base"),
                new("Helm equip: PhysDef +2", _helmEquipPhysDef,
                    _helmEquipPhysDef ? null : "PhysicalDefense did not increase by 2"),
                new("Helm equip: passives registered", _helmPassivesRegistered,
                    _helmPassivesRegistered ? null : "on_self_hit+shield passive not found"),
                new("Helm unequip: equip passives removed", _helmUnequipPassivesRemoved,
                    _helmUnequipPassivesRemoved ? null : "Equip passive still present after unequip"),
                new("Helm unequip: base passives survive", _helmUnequipBasePassivesSurvive,
                    _helmUnequipBasePassivesSurvive ? null : "Base unit passives were removed"),
                new("EquipFromInventory: item removed from inventory", _equipFromInventoryRemoved,
                    _equipFromInventoryRemoved ? null : "Item still in inventory after equip"),
                new("EquipFromInventory: item equipped", _equipFromInventoryEquipped,
                    _equipFromInventoryEquipped ? null : "Item not in equipment after equip"),
                new("UnequipToInventory: item returned to inventory", _unequipToInventoryReturned,
                    _unequipToInventoryReturned ? null : "Item not in inventory after unequip"),
                new("UnequipToInventory: slot cleared", _unequipToInventoryCleared,
                    _unequipToInventoryCleared ? null : "Equipment slot not cleared"),
                new("Duplicate equip same slot rejected", _wrongSlotRejected,
                    _wrongSlotRejected ? null : "Second equip to same slot should return false"),
                new("Dead entity: equipment cleared", _deadEntityCleanedUp,
                    _deadEntityCleanedUp ? null : "Equipment not cleared after EntityDiedEvent"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventBus?.ClearAllSubscriptions();
            _equipmentService = null;
            _entityStats = null;
            _passiveService = null;
            _inventoryService = null;
            _eventBus = null;
        }
    }
}
