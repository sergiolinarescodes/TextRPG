using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Luck
{
    public interface ILuckService
    {
        int GetLuck(EntityId entity);
        bool RollCritical(EntityId source);
        float GetCritDamageMultiplier(EntityId source);
        float AdjustChance(float baseChance, EntityId entity, bool isPositive);
    }
}
