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
        FrontEnemy,
        MiddleEnemy,
        BackEnemy,

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

        // === Subset targeting ===
        HalfEnemiesRandom,
        TwoRandomEnemies,
        ThreeRandomEnemies,
    }
}
