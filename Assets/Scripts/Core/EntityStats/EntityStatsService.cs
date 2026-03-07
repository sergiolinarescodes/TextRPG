using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Modifier;
using Unidad.Core.Systems;

namespace TextRPG.Core.EntityStats
{
    internal sealed class EntityStatsService : SystemServiceBase, IEntityStatsService
    {
        private readonly Dictionary<EntityId, EntityStatEntry> _entities = new();

        public EntityStatsService(IEventBus eventBus) : base(eventBus) { }

        public void RegisterEntity(EntityId id, int maxHealth, int strength, int magicPower,
                                   int physicalDefense, int magicDefense, int luck, int movementPoints = 0)
        {
            if (_entities.ContainsKey(id))
                throw new InvalidOperationException($"Entity '{id.Value}' is already registered.");

            var entry = new EntityStatEntry(maxHealth, strength, magicPower, physicalDefense, magicDefense, luck, movementPoints);
            _entities[id] = entry;
            Publish(new EntityRegisteredEvent(id, maxHealth));
        }

        public void RemoveEntity(EntityId id)
        {
            if (!_entities.Remove(id))
                throw new KeyNotFoundException($"Entity '{id.Value}' not found.");
            Publish(new EntityRemovedEvent(id));
        }

        public bool HasEntity(EntityId id) => _entities.ContainsKey(id);

        public int GetStat(EntityId id, StatType stat)
        {
            var entry = GetEntry(id);
            var baseStat = entry.GetBase(stat);
            var stack = entry.GetModifierStack(stat);
            return stack.Evaluate(baseStat);
        }

        public int GetBaseStat(EntityId id, StatType stat)
        {
            return GetEntry(id).GetBase(stat);
        }

        public int GetCurrentHealth(EntityId id)
        {
            return GetEntry(id).CurrentHealth;
        }

        public void ApplyDamage(EntityId id, int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));

            var entry = GetEntry(id);

            var shieldAbsorbed = Math.Min(amount, entry.CurrentShield);
            if (shieldAbsorbed > 0)
            {
                var previousShield = entry.CurrentShield;
                entry.CurrentShield -= shieldAbsorbed;
                Publish(new ShieldChangedEvent(id, entry.CurrentShield, previousShield));
            }

            var healthDamage = amount - shieldAbsorbed;
            if (healthDamage > 0)
            {
                entry.CurrentHealth = Math.Max(0, entry.CurrentHealth - healthDamage);
                Publish(new DamageTakenEvent(id, healthDamage, entry.CurrentHealth));

                if (entry.CurrentHealth == 0)
                    Publish(new EntityDiedEvent(id));
            }
        }

        public void ApplyHeal(EntityId id, int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Heal amount cannot be negative.", nameof(amount));

            var entry = GetEntry(id);
            var maxHealth = GetStat(id, StatType.MaxHealth);
            entry.CurrentHealth = Math.Min(maxHealth, entry.CurrentHealth + amount);
            Publish(new HealedEvent(id, amount, entry.CurrentHealth));
        }

        public void ApplyShield(EntityId id, int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Shield amount cannot be negative.", nameof(amount));

            var entry = GetEntry(id);
            var previousShield = entry.CurrentShield;
            entry.CurrentShield += amount;
            Publish(new ShieldChangedEvent(id, entry.CurrentShield, previousShield));
        }

        public int GetCurrentShield(EntityId id)
        {
            return GetEntry(id).CurrentShield;
        }

        public void AddModifier(EntityId id, StatType stat, IModifier<int> modifier)
        {
            var entry = GetEntry(id);
            entry.GetModifierStack(stat).Add(modifier);
            Publish(new StatModifierAddedEvent(id, stat, modifier.Id));
        }

        public bool RemoveModifier(EntityId id, StatType stat, string modifierId)
        {
            var entry = GetEntry(id);
            var removed = entry.GetModifierStack(stat).Remove(modifierId);
            if (removed)
                Publish(new StatModifierRemovedEvent(id, stat, modifierId));
            return removed;
        }

        private EntityStatEntry GetEntry(EntityId id)
        {
            if (!_entities.TryGetValue(id, out var entry))
                throw new KeyNotFoundException($"Entity '{id.Value}' not found.");
            return entry;
        }

        private sealed class EntityStatEntry
        {
            private readonly int[] _baseStats = new int[8];
            private readonly ModifierStack<int>[] _modifierStacks = new ModifierStack<int>[8];

            public int CurrentHealth { get; set; }
            public int CurrentShield { get; set; }

            public EntityStatEntry(int maxHealth, int strength, int magicPower,
                                   int physicalDefense, int magicDefense, int luck, int movementPoints)
            {
                _baseStats[(int)StatType.Health] = maxHealth;
                _baseStats[(int)StatType.MaxHealth] = maxHealth;
                _baseStats[(int)StatType.Strength] = strength;
                _baseStats[(int)StatType.MagicPower] = magicPower;
                _baseStats[(int)StatType.PhysicalDefense] = physicalDefense;
                _baseStats[(int)StatType.MagicDefense] = magicDefense;
                _baseStats[(int)StatType.Luck] = luck;
                _baseStats[(int)StatType.MovementPoints] = movementPoints;
                CurrentHealth = maxHealth;

                for (int i = 0; i < _modifierStacks.Length; i++)
                    _modifierStacks[i] = new ModifierStack<int>();
            }

            public int GetBase(StatType stat) => _baseStats[(int)stat];

            public ModifierStack<int> GetModifierStack(StatType stat) => _modifierStacks[(int)stat];
        }
    }
}
