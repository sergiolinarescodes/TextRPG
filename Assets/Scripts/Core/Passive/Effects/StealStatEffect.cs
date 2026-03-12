using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution.Handlers;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.Passive.Effects
{
    [AutoScan]
    internal sealed class StealStatEffect : IPassiveEffect
    {
        private static readonly StatType[] StealableStats =
        {
            StatType.Strength, StatType.MagicPower,
            StatType.PhysicalDefense, StatType.MagicDefense, StatType.Luck,
        };

        private readonly Random _rng = new();
        private int _nextId;

        public string EffectId => "steal_stat";

        public void Execute(EntityId owner, int value, string effectParam,
                            IReadOnlyList<EntityId> targets, IPassiveContext ctx)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!ctx.EntityStats.HasEntity(target) || ctx.EntityStats.GetCurrentHealth(target) <= 0) continue;

                var stat = StealableStats[_rng.Next(StealableStats.Length)];

                var debuffId = $"steal_stat_debuff_{_nextId}";
                var buffId = $"steal_stat_buff_{_nextId}";
                _nextId++;

                ctx.EntityStats.AddModifier(target, stat, new StatBuffModifier(debuffId, -value));
                ctx.EntityStats.AddModifier(owner, stat, new StatBuffModifier(buffId, value));
            }
        }
    }
}
