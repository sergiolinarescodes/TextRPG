using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Consumable
{
    internal sealed class ConsumableService : SystemServiceBase, IConsumableService
    {
        private readonly IConsumableRegistry _registry;
        private readonly Dictionary<EntityId, ConsumableStateEntry> _equipped = new();

        public ConsumableService(IEventBus eventBus, IConsumableRegistry registry) : base(eventBus)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void EquipConsumable(EntityId entity, string itemWord)
        {
            var key = itemWord.ToLowerInvariant();
            if (!_registry.TryGet(key, out var def))
                return;

            _equipped[entity] = new ConsumableStateEntry(def);
            Publish(new ConsumableEquippedEvent(entity, def));
        }

        public bool UseConsumable(EntityId entity)
        {
            if (!_equipped.TryGetValue(entity, out var state))
                return false;

            state.CurrentDurability--;
            Publish(new ConsumableDurabilityChangedEvent(entity, state.Definition.Word,
                state.CurrentDurability, state.Definition.Durability));

            if (state.CurrentDurability <= 0)
            {
                var word = state.Definition.Word;
                _equipped.Remove(entity);
                Publish(new ConsumableDestroyedEvent(entity, word));
            }

            return true;
        }

        public bool HasConsumable(EntityId entity) => _equipped.ContainsKey(entity);

        public ConsumableDefinition? GetDefinition(EntityId entity)
        {
            return _equipped.TryGetValue(entity, out var state) ? state.Definition : null;
        }

        public int GetDurability(EntityId entity)
        {
            return _equipped.TryGetValue(entity, out var state) ? state.CurrentDurability : 0;
        }

        public bool IsAmmoForEquipped(EntityId entity, string ammoWord)
        {
            if (!_equipped.TryGetValue(entity, out var state))
                return false;

            var key = ammoWord.ToLowerInvariant();
            var ammoWords = state.Definition.AmmoWords;
            for (int i = 0; i < ammoWords.Count; i++)
            {
                if (string.Equals(ammoWords[i], key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetAmmoWords(EntityId entity)
        {
            return _equipped.TryGetValue(entity, out var state)
                ? state.Definition.AmmoWords
                : Array.Empty<string>();
        }

        private sealed class ConsumableStateEntry
        {
            public ConsumableDefinition Definition { get; }
            public int CurrentDurability { get; set; }

            public ConsumableStateEntry(ConsumableDefinition definition)
            {
                Definition = definition;
                CurrentDurability = definition.Durability;
            }
        }
    }
}
