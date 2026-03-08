using TextRPG.Core.EntityStats;

namespace TextRPG.Core.CombatAI
{
    public interface ICombatAIService
    {
        void ProcessTurn(EntityId entityId);
    }
}
