using System;
using System.Collections.Generic;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Modifier;
using Unidad.Core.Systems;

namespace TextRPG.Core.EntityStats
{
    internal sealed class EntityStatsService : SystemServiceBase, IEntityStatsService
    {
        private readonly Dictionary<EntityId, EntityStatEntry> _entities = new();

        public EntityStatsService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<TurnStartedEvent>(OnTurnStarted);
        }

        public void RegisterEntity(EntityId id, int maxHealth, int strength, int magicPower,
                                   int physicalDefense, int magicDefense, int luck,
                                   int maxMana = 10, int manaRegen = 2, int startingMana = 5,
                                   int startingShield = 0, int dexterity = 0,
                                   int constitution = 0)
        {
            if (_entities.ContainsKey(id))
                throw new InvalidOperationException($"Entity '{id.Value}' is already registered.");

            var hpBonus = CalculateConstitutionBonus(constitution);
            var effectiveMaxHealth = maxHealth + hpBonus;
            var entry = new EntityStatEntry(effectiveMaxHealth, strength, magicPower, physicalDefense, magicDefense, luck, maxMana, manaRegen, startingMana, startingShield, dexterity, constitution);
            _entities[id] = entry;
            Publish(new EntityRegisteredEvent(id, effectiveMaxHealth));
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

        public void ApplyDamage(EntityId id, int amount, EntityId? damageSource = null)
        {
            if (amount < 0)
                throw new ArgumentException("Damage amount cannot be negative.", nameof(amount));

            var entry = GetEntry(id);

            var reduction = GetStat(id, StatType.DamageReduction);
            if (reduction > 0)
                amount = Math.Max(0, amount - reduction);
            if (amount == 0)
                return;

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
                Publish(new DamageTakenEvent(id, healthDamage, entry.CurrentHealth, damageSource));

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

        public int GetCurrentMana(EntityId id)
        {
            return GetEntry(id).CurrentMana;
        }

        public void ApplyMana(EntityId id, int amount)
        {
            var entry = GetEntry(id);
            var maxMana = GetStat(id, StatType.MaxMana);
            var previous = entry.CurrentMana;
            entry.CurrentMana = Math.Min(maxMana, entry.CurrentMana + amount);
            Publish(new ManaChangedEvent(id, entry.CurrentMana, previous));
        }

        public bool TrySpendMana(EntityId id, int cost)
        {
            var entry = GetEntry(id);
            if (entry.CurrentMana < cost)
            {
                Publish(new ManaInsufficientEvent(id, cost, entry.CurrentMana));
                return false;
            }
            var previous = entry.CurrentMana;
            entry.CurrentMana -= cost;
            Publish(new ManaChangedEvent(id, entry.CurrentMana, previous));
            return true;
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

        private void OnTurnStarted(TurnStartedEvent e)
        {
            if (!_entities.ContainsKey(e.EntityId)) return;
            var regen = GetStat(e.EntityId, StatType.ManaRegen);
            if (regen > 0) ApplyMana(e.EntityId, regen);
        }

        private EntityStatEntry GetEntry(EntityId id)
        {
            if (!_entities.TryGetValue(id, out var entry))
                throw new KeyNotFoundException($"Entity '{id.Value}' not found.");
            return entry;
        }

        private static int CalculateConstitutionBonus(int constitution)
            => constitution > 0 ? constitution * (constitution + 1) / 2 + 2 : 0;

        private sealed class EntityStatEntry
        {
            private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;
            private readonly int[] _baseStats = new int[StatCount];
            private readonly ModifierStack<int>[] _modifierStacks = new ModifierStack<int>[StatCount];

            public int CurrentHealth { get; set; }
            public int CurrentShield { get; set; }
            public int CurrentMana { get; set; }

            public EntityStatEntry(int maxHealth, int strength, int magicPower,
                                   int physicalDefense, int magicDefense, int luck,
                                   int maxMana, int manaRegen, int startingMana, int startingShield, int dexterity, int constitution)
            {
                _baseStats[(int)StatType.Health] = maxHealth;
                _baseStats[(int)StatType.MaxHealth] = maxHealth;
                _baseStats[(int)StatType.Strength] = strength;
                _baseStats[(int)StatType.MagicPower] = magicPower;
                _baseStats[(int)StatType.PhysicalDefense] = physicalDefense;
                _baseStats[(int)StatType.MagicDefense] = magicDefense;
                _baseStats[(int)StatType.Luck] = luck;
                _baseStats[(int)StatType.MaxMana] = maxMana;
                _baseStats[(int)StatType.ManaRegen] = manaRegen;
                _baseStats[(int)StatType.Dexterity] = dexterity;
                _baseStats[(int)StatType.Constitution] = constitution;
                CurrentHealth = maxHealth;
                CurrentMana = startingMana;
                CurrentShield = startingShield;

                for (int i = 0; i < _modifierStacks.Length; i++)
                    _modifierStacks[i] = new ModifierStack<int>();
            }

            public int GetBase(StatType stat) => _baseStats[(int)stat];

            public ModifierStack<int> GetModifierStack(StatType stat) => _modifierStacks[(int)stat];
        }
    }
}
