using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EnemyAI
{
    public interface IEnemyAIService
    {
        void ProcessTurn(EntityId enemyId);
    }
}
