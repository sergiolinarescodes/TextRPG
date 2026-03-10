using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Encounter
{
    internal static class EntityRegistrationHelper
    {
        public static void RegisterFromDefinition(
            IEntityStatsService stats, EntityId id, EntityDefinition def)
            => stats.RegisterEntity(id, def.MaxHealth, def.Strength, def.MagicPower,
                def.PhysicalDefense, def.MagicDefense, def.Luck,
                startingShield: def.StartingShield,
                dexterity: def.Dexterity, constitution: def.Constitution);
    }
}
