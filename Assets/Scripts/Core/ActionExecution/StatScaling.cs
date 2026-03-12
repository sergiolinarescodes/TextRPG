using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution
{
    internal static class StatScaling
    {
        public const int DefaultDivisor = 3;
        public const int WeakDivisor = 6;

        public static int OffensiveScale(int baseValue, int offensiveStat, int defensiveStat, int divisor = DefaultDivisor)
            => Math.Max(1, baseValue + offensiveStat / divisor - defensiveStat / divisor);

        public static int SupportScale(int baseValue, int scalingStat, int divisor = DefaultDivisor)
            => baseValue + scalingStat / divisor;

        public static int ApplyCritical(int damage, ActionContext context, ILuckService luckService, IEventBus eventBus, EntityId target)
        {
            if (!context.IsCritical || luckService == null) return damage;
            int original = damage;
            damage = (int)(damage * luckService.GetCritDamageMultiplier(context.Source));
            eventBus?.Publish(new CriticalHitEvent(context.Source, target, original, damage));
            return damage;
        }
    }
}
