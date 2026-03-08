using Unidad.Core.Patterns.Modifier;

namespace TextRPG.Core.EntityStats
{
    public interface IEntityStatsService
    {
        void RegisterEntity(EntityId id, int maxHealth, int strength, int magicPower,
                            int physicalDefense, int magicDefense, int luck,
                            int maxMana = 10, int manaRegen = 2, int startingMana = 5,
                            int startingShield = 0);
        void RemoveEntity(EntityId id);
        bool HasEntity(EntityId id);

        int GetStat(EntityId id, StatType stat);
        int GetBaseStat(EntityId id, StatType stat);
        int GetCurrentHealth(EntityId id);

        void ApplyDamage(EntityId id, int amount, EntityId? damageSource = null);
        void ApplyHeal(EntityId id, int amount);

        void ApplyShield(EntityId id, int amount);
        int GetCurrentShield(EntityId id);

        int GetCurrentMana(EntityId id);
        void ApplyMana(EntityId id, int amount);
        bool TrySpendMana(EntityId id, int cost);

        void AddModifier(EntityId id, StatType stat, IModifier<int> modifier);
        bool RemoveModifier(EntityId id, StatType stat, string modifierId);
    }
}
