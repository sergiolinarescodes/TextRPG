using Unidad.Core.Patterns.Modifier;

namespace TextRPG.Core.EntityStats
{
    public interface IEntityStatsService
    {
        void RegisterEntity(EntityId id, int maxHealth, int strength, int magicPower,
                            int physicalDefense, int magicDefense, int luck, int movementPoints = 0);
        void RemoveEntity(EntityId id);
        bool HasEntity(EntityId id);

        int GetStat(EntityId id, StatType stat);
        int GetBaseStat(EntityId id, StatType stat);
        int GetCurrentHealth(EntityId id);

        void ApplyDamage(EntityId id, int amount);
        void ApplyHeal(EntityId id, int amount);

        void ApplyShield(EntityId id, int amount);
        int GetCurrentShield(EntityId id);

        void AddModifier(EntityId id, StatType stat, IModifier<int> modifier);
        bool RemoveModifier(EntityId id, StatType stat, string modifierId);
    }
}
