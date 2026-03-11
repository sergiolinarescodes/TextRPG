using TextRPG.Core.PlayerClass;

namespace TextRPG.Core.EntityStats
{
    public static class PlayerDefaults
    {
        public const int MaxHealth = 50;
        public const int Strength = 6;
        public const int MagicPower = 5;
        public const int PhysicalDefense = 3;
        public const int MagicDefense = 3;
        public const int Luck = 3;
        public const int MaxMana = 8;
        public const int ManaRegen = 2;
        public const int StartingMana = 4;
        public const int Constitution = 3;

        public static void Register(IEntityStatsService stats, EntityId playerId)
            => stats.RegisterEntity(playerId, MaxHealth, Strength, MagicPower,
                PhysicalDefense, MagicDefense, Luck,
                MaxMana, ManaRegen, StartingMana,
                constitution: Constitution);

        public static void Register(IEntityStatsService stats, EntityId playerId, ClassDefinition classDef)
            => stats.RegisterEntity(playerId, classDef.MaxHealth, classDef.Strength, classDef.MagicPower,
                classDef.PhysicalDefense, classDef.MagicDefense, classDef.Luck,
                classDef.MaxMana, classDef.ManaRegen, classDef.StartingMana,
                constitution: classDef.Constitution);
    }
}
