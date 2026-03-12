using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Luck
{
    internal sealed class LuckService : ILuckService
    {
        private readonly IEntityStatsService _entityStats;

        public LuckService(IEntityStatsService entityStats)
        {
            _entityStats = entityStats;
        }

        public int GetLuck(EntityId entity)
        {
            return _entityStats.GetStat(entity, StatType.Luck);
        }

        public bool RollCritical(EntityId source)
        {
            int luck = GetLuck(source);
            int chance = Math.Min(95, luck * 3);
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        public float GetCritDamageMultiplier(EntityId source)
        {
            int critDamage = _entityStats.GetStat(source, StatType.CriticalDamage);
            return 1.0f + critDamage / 100f;
        }

        public float AdjustChance(float baseChance, EntityId entity, bool isPositive)
        {
            int luck = GetLuck(entity);
            float luckBonus = luck * 0.01f;

            if (isPositive)
                return Math.Min(1.0f, baseChance + luckBonus);
            else
                return Math.Max(0.0f, baseChance - luckBonus);
        }
    }
}
