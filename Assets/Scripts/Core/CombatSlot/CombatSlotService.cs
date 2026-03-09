using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.CombatSlot
{
    internal sealed class CombatSlotService : SystemServiceBase, ICombatSlotService
    {
        private EntityId?[] _enemies;
        private EntityId?[] _allies;
        private readonly Dictionary<EntityId, CombatSlot> _entitySlots = new();

        public CombatSlotService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<EntityDiedEvent>(e => RemoveEntity(e.EntityId));
        }

        public void Initialize(int maxEnemySlots = 3, int maxAllySlots = 2)
        {
            _enemies = new EntityId?[maxEnemySlots];
            _allies = new EntityId?[maxAllySlots];
            _entitySlots.Clear();
        }

        public void RegisterEnemy(EntityId entityId, int slotIndex)
        {
            if (_enemies == null) throw new InvalidOperationException("Not initialized.");
            if (slotIndex < 0 || slotIndex >= _enemies.Length) return;
            _enemies[slotIndex] = entityId;
            var slot = new CombatSlot(SlotType.Enemy, slotIndex);
            _entitySlots[entityId] = slot;
            Publish(new SlotEntityRegisteredEvent(entityId, slot));
        }

        public void RegisterAlly(EntityId entityId, int slotIndex)
        {
            if (_allies == null) throw new InvalidOperationException("Not initialized.");
            if (slotIndex < 0 || slotIndex >= _allies.Length) return;
            _allies[slotIndex] = entityId;
            var slot = new CombatSlot(SlotType.Ally, slotIndex);
            _entitySlots[entityId] = slot;
            Publish(new SlotEntityRegisteredEvent(entityId, slot));
        }

        public void RemoveEntity(EntityId entityId)
        {
            if (!_entitySlots.TryGetValue(entityId, out var slot)) return;
            var arr = slot.Type == SlotType.Enemy ? _enemies : _allies;
            if (arr != null && slot.Index >= 0 && slot.Index < arr.Length)
                arr[slot.Index] = null;
            _entitySlots.Remove(entityId);
            Publish(new SlotEntityRemovedEvent(entityId, slot));
        }

        public CombatSlot? GetSlot(EntityId entityId)
        {
            return _entitySlots.TryGetValue(entityId, out var slot) ? slot : null;
        }

        public EntityId? GetEntityAt(SlotType type, int index)
        {
            var arr = type == SlotType.Enemy ? _enemies : _allies;
            if (arr == null || index < 0 || index >= arr.Length) return null;
            return arr[index];
        }

        public IReadOnlyList<EntityId> GetAllEnemies()
        {
            var result = new List<EntityId>();
            if (_enemies == null) return result;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i].HasValue)
                    result.Add(_enemies[i].Value);
            }
            return result;
        }

        public IReadOnlyList<EntityId> GetAllAllies()
        {
            var result = new List<EntityId>();
            if (_allies == null) return result;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i].HasValue)
                    result.Add(_allies[i].Value);
            }
            return result;
        }

        public EntityId? FindNearestOccupiedSlot(SlotType type, int targetIndex)
        {
            var arr = type == SlotType.Enemy ? _enemies : _allies;
            if (arr == null) return null;

            if (targetIndex >= 0 && targetIndex < arr.Length && arr[targetIndex].HasValue)
                return arr[targetIndex].Value;

            for (int offset = 1; offset < arr.Length; offset++)
            {
                var lo = targetIndex - offset;
                var hi = targetIndex + offset;
                if (lo >= 0 && lo < arr.Length && arr[lo].HasValue)
                    return arr[lo].Value;
                if (hi >= 0 && hi < arr.Length && arr[hi].HasValue)
                    return arr[hi].Value;
            }
            return null;
        }

        public int GetOccupiedEnemyCount()
        {
            int count = 0;
            if (_enemies == null) return count;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i].HasValue) count++;
            }
            return count;
        }

        public int GetOccupiedAllyCount()
        {
            int count = 0;
            if (_allies == null) return count;
            for (int i = 0; i < _allies.Length; i++)
            {
                if (_allies[i].HasValue) count++;
            }
            return count;
        }

        public int? FindFirstEmptySlot(SlotType type)
        {
            var arr = type == SlotType.Enemy ? _enemies : _allies;
            if (arr == null) return null;
            for (int i = 0; i < arr.Length; i++)
            {
                if (!arr[i].HasValue) return i;
            }
            return null;
        }
    }
}
