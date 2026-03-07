namespace TextRPG.Core.ActionExecution
{
    public enum TargetType
    {
        // === Basic ===
        Self,
        SingleEnemy,
        AllEnemies,
        All,
        AllAllies,
        AllAlliesAndSelf,

        // === Positional ===
        Melee,
        Area,

        // === Random ===
        RandomEnemy,
        RandomAlly,
        RandomAny,

        // === Stat-based single target ===
        LowestHealthEnemy,
        HighestHealthEnemy,
        LowestDefenseEnemy,
        HighestDefenseEnemy,
        LowestStrengthEnemy,
        HighestStrengthEnemy,
        LowestMagicEnemy,
        HighestMagicEnemy,

        // === Random + stat tiebreakers ===
        RandomLowestHealthEnemy,
        RandomHighestHealthEnemy,

        // === Status-based: any status effect ===
        RandomEnemyWithStatus,
        RandomEnemyWithoutStatus,
        AllEnemiesWithStatus,
        AllEnemiesWithoutStatus,

        // === Status-based: specific effects ===
        AllBurningEnemies,
        AllWetEnemies,
        AllPoisonedEnemies,
        AllFrozenEnemies,
        AllStunnedEnemies,
        AllCursedEnemies,
        AllFearfulEnemies,

        // === Status + random ===
        RandomBurningEnemy,
        RandomWetEnemy,
        RandomPoisonedEnemy,
        RandomFrozenEnemy,
        RandomStunnedEnemy,

        // === Status + stat ===
        LowestHealthBurningEnemy,
        LowestHealthPoisonedEnemy,
        LowestHealthWetEnemy,

        // === Subset targeting ===
        HalfEnemiesRandom,
        TwoRandomEnemies,
        ThreeRandomEnemies,
    }
}
