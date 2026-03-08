using TextRPG.Core.EntityStats;

namespace TextRPG.Core.CombatAI
{
    public sealed class AIDecisionContext
    {
        public EntityId EntityId { get; }
        public EntityId TargetId { get; }
        public string AbilityWord { get; }
        public int EntityHealth { get; }
        public int EntityMaxHealth { get; }
        public int TargetHealth { get; }
        public int TargetMaxHealth { get; }
        public int Distance { get; }
        public bool IsMelee { get; }
        public int Range { get; }

        public AIDecisionContext(EntityId entityId, EntityId targetId, string abilityWord,
            int entityHealth, int entityMaxHealth, int targetHealth, int targetMaxHealth,
            int distance, bool isMelee, int range = 1)
        {
            EntityId = entityId;
            TargetId = targetId;
            AbilityWord = abilityWord;
            EntityHealth = entityHealth;
            EntityMaxHealth = entityMaxHealth;
            TargetHealth = targetHealth;
            TargetMaxHealth = targetMaxHealth;
            Distance = distance;
            IsMelee = isMelee;
            Range = range;
        }
    }
}
