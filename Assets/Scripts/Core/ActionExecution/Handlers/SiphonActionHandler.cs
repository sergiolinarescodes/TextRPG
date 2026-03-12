using System;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class SiphonActionHandler : IActionHandler
    {
        private static readonly StatType[] StealableStats =
        {
            StatType.Strength, StatType.MagicPower,
            StatType.PhysicalDefense, StatType.MagicDefense, StatType.Luck,
        };

        private readonly IEntityStatsService _entityStats;
        private readonly IEventBus _eventBus;
        private readonly Random _rng = new();
        private int _nextId;

        public string ActionId => ActionNames.Siphon;

        public SiphonActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var stat = StealableStats[_rng.Next(StealableStats.Length)];

                var debuffId = $"siphon_debuff_{_nextId}";
                var buffId = $"siphon_buff_{_nextId}";
                _nextId++;

                _entityStats.AddModifier(target, stat, new StatBuffModifier(debuffId, -context.Value));
                _entityStats.AddModifier(context.Source, stat, new StatBuffModifier(buffId, context.Value));
                _eventBus.Publish(new StatSiphonedEvent(context.Source, target, context.Value));
            }
        }
    }
}
