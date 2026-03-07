using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class StatModifierActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly StatType _statType;
        private readonly bool _isBuff;
        private int _nextId;

        public string ActionId { get; }

        public StatModifierActionHandler(string actionId, IActionHandlerContext ctx, StatType statType, bool isBuff)
        {
            ActionId = actionId;
            _entityStats = ctx.EntityStats;
            _statType = statType;
            _isBuff = isBuff;
        }

        public void Execute(ActionContext context)
        {
            var amount = _isBuff ? context.Value : -context.Value;

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var modId = $"{ActionId}_{_nextId++}";
                _entityStats.AddModifier(context.Targets[i], _statType, new StatBuffModifier(modId, amount));
            }
        }
    }
}
