using System;

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
    }
}
