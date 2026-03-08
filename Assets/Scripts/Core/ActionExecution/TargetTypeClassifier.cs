using System;
using System.Collections.Generic;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct TargetSpec(TargetType BaseType, StatusEffectType? StatusFilter);

    internal static class TargetTypeClassifier
    {
        private static readonly Dictionary<string, TargetType> Aliases = new()
        {
            ["AreaEnemies"] = TargetType.AllEnemies,
            ["AreaAll"] = TargetType.All,
            ["Front"] = TargetType.FrontEnemy,
            ["Middle"] = TargetType.MiddleEnemy,
            ["Back"] = TargetType.BackEnemy,
        };

        public static TargetSpec Parse(string target)
        {
            var plusIdx = target.IndexOf('+');
            if (plusIdx >= 0)
            {
                var basePart = target.Substring(0, plusIdx);
                var statusPart = target.Substring(plusIdx + 1);
                var baseType = ResolveBaseType(basePart);
                if (Enum.TryParse<StatusEffectType>(statusPart, out var statusType))
                    return new TargetSpec(baseType, statusType);
                return new TargetSpec(baseType, null);
            }

            return new TargetSpec(ResolveBaseType(target), null);
        }

        public static TargetType ParseTargetType(string target)
        {
            return Parse(target).BaseType;
        }

        private static TargetType ResolveBaseType(string target)
        {
            if (Aliases.TryGetValue(target, out var aliased))
                return aliased;
            if (Enum.TryParse<TargetType>(target, out var parsed))
                return parsed;
            return TargetType.SingleEnemy;
        }
    }
}
