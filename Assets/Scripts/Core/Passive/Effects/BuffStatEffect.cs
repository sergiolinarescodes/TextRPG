using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class BuffStatEffect : IPassiveEffect
    {
        private int _nextId;

        public string EffectId => "buff_stat";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            if (string.IsNullOrEmpty(effectParam)) return;
            if (!Enum.TryParse<StatType>(effectParam, out var stat)) return;
            if (stat is StatType.Health or StatType.MaxHealth) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target)) continue;

                var modId = $"buff_stat_{_nextId++}";
                ctx.EntityStats.AddModifier(target, stat, new StatBuffModifier(modId, value));
            }
        }
    }
}
