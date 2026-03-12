using System.Collections.Generic;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Consumable;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Systems;

namespace TextRPG.Core.Equipment
{
    internal sealed class EquipmentService : SystemServiceBase, IEquipmentService
    {
        private readonly IItemRegistry _itemRegistry;
        private readonly IEntityStatsService _entityStats;
        private readonly IPassiveService _passiveService;
        private readonly IWeaponService _weaponService;
        private readonly IConsumableService _consumableService;
        private readonly Dictionary<EntityId, Dictionary<EquipmentSlotType, EquipmentEntry>> _equipped = new();
        private readonly HashSet<EquipmentSlotType> _lockedSlots = new();
        private int _nextModifierId;
        private bool _inBattle;
        private bool _setupPhase;

        public EquipmentService(
            IEventBus eventBus,
            IItemRegistry itemRegistry,
            IEntityStatsService entityStats,
            IPassiveService passiveService,
            IWeaponService weaponService,
            IConsumableService consumableService = null) : base(eventBus)
        {
            _itemRegistry = itemRegistry;
            _entityStats = entityStats;
            _passiveService = passiveService;
            _weaponService = weaponService;
            _consumableService = consumableService;

            Subscribe<EntityDiedEvent>(evt => _equipped.Remove(evt.EntityId));
            Subscribe<WeaponDestroyedEvent>(evt => Unequip(evt.Entity, EquipmentSlotType.Weapon));
            Subscribe<ConsumableDestroyedEvent>(evt => Unequip(evt.Entity, EquipmentSlotType.Consumable));
            Subscribe<PlayerTurnEndedEvent>(_ =>
            {
                if (_inBattle && _setupPhase)
                    _setupPhase = false;
            });
        }

        public bool IsInBattle => _inBattle;
        public bool IsBattleSetupPhase => _setupPhase;

        public bool CanEquipSlotInBattle(EquipmentSlotType slot)
            => !_inBattle || _setupPhase || !_lockedSlots.Contains(slot);

        public bool CanUnequipInBattle()
            => !_inBattle || _setupPhase;

        public void EnterBattle()
        {
            _inBattle = true;
            _setupPhase = true;
            _lockedSlots.Clear();
        }

        public void ExitBattle()
        {
            _inBattle = false;
            _setupPhase = false;
            _lockedSlots.Clear();
        }

        public bool Equip(EntityId entity, string itemWord)
        {
            if (!_itemRegistry.TryGet(itemWord, out var itemDef)) return false;
            var slot = itemDef.SlotType;

            var slots = GetOrCreateSlots(entity);
            if (slots.ContainsKey(slot)) return false;

            var modifierIds = ApplyStatModifiers(entity, itemDef);
            slots[slot] = new EquipmentEntry(itemDef, modifierIds);

            if (itemDef.Passives != null && itemDef.Passives.Length > 0)
                _passiveService?.RegisterPassives(entity, $"equip:{itemDef.ItemWord}", itemDef.Passives);

            if (slot == EquipmentSlotType.Weapon)
                _weaponService?.EquipWeapon(entity, itemWord);

            if (slot == EquipmentSlotType.Consumable)
                _consumableService?.EquipConsumable(entity, itemWord);

            if (_inBattle && !_setupPhase)
                _lockedSlots.Add(slot);

            Publish(new ItemEquippedEvent(entity, itemDef, slot));
            return true;
        }

        public bool Unequip(EntityId entity, EquipmentSlotType slot)
        {
            if (!_equipped.TryGetValue(entity, out var slots)) return false;
            if (!slots.TryGetValue(slot, out var entry)) return false;

            RemoveStatModifiers(entity, entry.ModifierIds);
            slots.Remove(slot);

            if (entry.Item.Passives != null && entry.Item.Passives.Length > 0)
                _passiveService?.RemovePassives(entity, $"equip:{entry.Item.ItemWord}");

            Publish(new ItemUnequippedEvent(entity, entry.Item, slot));
            return true;
        }

        public bool HasEquipped(EntityId entity, EquipmentSlotType slot)
        {
            return _equipped.TryGetValue(entity, out var slots) && slots.ContainsKey(slot);
        }

        public EquipmentItemDefinition GetEquipped(EntityId entity, EquipmentSlotType slot)
        {
            if (_equipped.TryGetValue(entity, out var slots) && slots.TryGetValue(slot, out var entry))
                return entry.Item;
            return null;
        }

        public bool IsSlotEmpty(EntityId entity, EquipmentSlotType slot)
        {
            return !HasEquipped(entity, slot);
        }

        public EquipmentSlotType? GetSlotTypeForItem(string itemWord)
        {
            if (_itemRegistry.TryGet(itemWord, out var def))
                return def.SlotType;
            return null;
        }

        public bool EquipFromInventory(EntityId entity, string itemWord, IInventoryService inventory, InventoryId inventoryId)
        {
            var slotType = GetSlotTypeForItem(itemWord);
            if (slotType == null) return false;

            // If slot occupied, swap: unequip current → inventory, then equip new
            if (HasEquipped(entity, slotType.Value))
            {
                var current = GetEquipped(entity, slotType.Value);
                Unequip(entity, slotType.Value);
                inventory.Add(inventoryId, new ItemId(current.ItemWord));
            }

            inventory.TryRemove(inventoryId, new ItemId(itemWord));
            var result = Equip(entity, itemWord);

            if (result && _inBattle && !_setupPhase)
                _lockedSlots.Add(slotType.Value);

            return result;
        }

        public bool UnequipToInventory(EntityId entity, EquipmentSlotType slot, IInventoryService inventory, InventoryId inventoryId)
        {
            var equipped = GetEquipped(entity, slot);
            if (equipped == null) return false;

            if (inventory.IsFull(inventoryId)) return false;

            Unequip(entity, slot);
            inventory.Add(inventoryId, new ItemId(equipped.ItemWord));
            return true;
        }

        private Dictionary<EquipmentSlotType, EquipmentEntry> GetOrCreateSlots(EntityId entity)
        {
            if (!_equipped.TryGetValue(entity, out var slots))
            {
                slots = new Dictionary<EquipmentSlotType, EquipmentEntry>();
                _equipped[entity] = slots;
            }
            return slots;
        }

        private List<(StatType stat, string modId)> ApplyStatModifiers(EntityId entity, EquipmentItemDefinition item)
        {
            var modifiers = new List<(StatType, string)>();
            var stats = item.Stats;

            void TryAdd(StatType stat, int value)
            {
                if (value == 0) return;
                var modId = $"equip_{item.ItemWord}_{stat}_{_nextModifierId++}";
                _entityStats.AddModifier(entity, stat, new StatBuffModifier(modId, value));
                modifiers.Add((stat, modId));
            }

            TryAdd(StatType.Strength, stats.Strength);
            TryAdd(StatType.MagicPower, stats.MagicPower);
            TryAdd(StatType.PhysicalDefense, stats.PhysDefense);
            TryAdd(StatType.MagicDefense, stats.MagicDefense);
            TryAdd(StatType.Luck, stats.Luck);
            TryAdd(StatType.MaxHealth, stats.MaxHealth);
            TryAdd(StatType.MaxMana, stats.MaxMana);

            return modifiers;
        }

        private void RemoveStatModifiers(EntityId entity, List<(StatType stat, string modId)> modifierIds)
        {
            foreach (var (stat, modId) in modifierIds)
                _entityStats.RemoveModifier(entity, stat, modId);
        }

        private sealed class EquipmentEntry
        {
            public EquipmentItemDefinition Item { get; }
            public List<(StatType stat, string modId)> ModifierIds { get; }

            public EquipmentEntry(EquipmentItemDefinition item, List<(StatType, string)> modifierIds)
            {
                Item = item;
                ModifierIds = modifierIds;
            }
        }
    }
}
