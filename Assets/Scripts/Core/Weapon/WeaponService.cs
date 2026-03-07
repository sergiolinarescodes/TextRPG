using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponService : SystemServiceBase, IWeaponService
    {
        private readonly IWeaponRegistry _registry;
        private readonly Dictionary<EntityId, WeaponStateEntry> _equipped = new();

        public WeaponService(IEventBus eventBus, IWeaponRegistry registry) : base(eventBus)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void EquipWeapon(EntityId entity, string weaponWord)
        {
            var key = weaponWord.ToLowerInvariant();
            if (!_registry.TryGet(key, out var def))
                return;

            _equipped[entity] = new WeaponStateEntry(def);
            Publish(new WeaponEquippedEvent(entity, def));
        }

        public bool UseWeapon(EntityId entity)
        {
            if (!_equipped.TryGetValue(entity, out var state))
                return false;

            state.CurrentDurability--;
            Publish(new WeaponDurabilityChangedEvent(entity, state.Definition.WeaponWord,
                state.CurrentDurability, state.Definition.Durability));

            if (state.CurrentDurability <= 0)
            {
                var weaponWord = state.Definition.WeaponWord;
                _equipped.Remove(entity);
                Publish(new WeaponDestroyedEvent(entity, weaponWord));
            }

            return true;
        }

        public bool HasWeapon(EntityId entity) => _equipped.ContainsKey(entity);

        public WeaponDefinition? GetWeaponDefinition(EntityId entity)
        {
            return _equipped.TryGetValue(entity, out var state) ? state.Definition : null;
        }

        public int GetCurrentDurability(EntityId entity)
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
    }
}
