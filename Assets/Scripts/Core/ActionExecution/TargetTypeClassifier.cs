using System;
using System.Collections.Generic;

namespace TextRPG.Core.ActionExecution
{
    internal static class TargetTypeClassifier
    {
        private static readonly Dictionary<string, TargetType> Aliases = new()
        {
            ["AreaEnemies"] = TargetType.AllEnemies,
            ["AreaAll"] = TargetType.All,
        };

        public static TargetType ParseTargetType(string target)
        {
            if (Aliases.TryGetValue(target, out var aliased))
                return aliased;
            if (Enum.TryParse<TargetType>(target, out var parsed))
                return parsed;
            return TargetType.SingleEnemy;
        }
    }
}
