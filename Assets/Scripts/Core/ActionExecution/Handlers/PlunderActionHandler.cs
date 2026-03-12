using System;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class PlunderActionHandler : IActionHandler
    {
        private static readonly StatType[] StealableStats =
        {
            StatType.Strength, StatType.MagicPower,
            StatType.PhysicalDefense, StatType.MagicDefense, StatType.Luck,
        };

        private readonly IEntityStatsService _entityStats;
        private readonly IEventBus _eventBus;
        private readonly ILuckService _luckService;
        private readonly Random _rng = new();
        private int _nextId;

        public string ActionId => ActionNames.Plunder;

        public PlunderActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _eventBus = ctx.EventBus;
            _luckService = ctx.LuckService;
        }

        public void Execute(ActionContext context)
        {
            var sourceStr = _entityStats.GetStat(context.Source, StatType.Strength);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];

                // Physical damage
                int damage = StatScaling.OffensiveScale(context.Value, sourceStr,
                    _entityStats.GetStat(target, StatType.PhysicalDefense));

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage);

                // Steal one random stat
                var stat = StealableStats[_rng.Next(StealableStats.Length)];
                var id = _nextId++;
                _entityStats.AddModifier(target, stat, new StatBuffModifier($"plunder_debuff_{id}", -1));
                _entityStats.AddModifier(context.Source, stat, new StatBuffModifier($"plunder_buff_{id}", 1));
            }
        }
    }
}
