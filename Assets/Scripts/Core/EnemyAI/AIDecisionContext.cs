using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EnemyAI
{
    public sealed class AIDecisionContext
    {
        public EntityId EnemyId { get; }
        public EntityId TargetId { get; }
        public string AbilityWord { get; }
        public int EnemyHealth { get; }
        public int EnemyMaxHealth { get; }
        public int TargetHealth { get; }
        public int TargetMaxHealth { get; }
        public int Distance { get; }
        public bool IsMelee { get; }

        public AIDecisionContext(EntityId enemyId, EntityId targetId, string abilityWord,
            int enemyHealth, int enemyMaxHealth, int targetHealth, int targetMaxHealth,
            int distance, bool isMelee)
        {
            EnemyId = enemyId;
            TargetId = targetId;
            AbilityWord = abilityWord;
            EnemyHealth = enemyHealth;
            EnemyMaxHealth = enemyMaxHealth;
            TargetHealth = targetHealth;
            TargetMaxHealth = targetMaxHealth;
            Distance = distance;
            IsMelee = isMelee;
        }
    }
}
