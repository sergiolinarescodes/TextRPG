using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ScaledDamageHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly StatType _offensiveStat;
        private readonly StatType _defensiveStat;

        public string ActionId { get; }

        public ScaledDamageHandler(string actionId, IActionHandlerContext ctx,
            StatType offensiveStat, StatType defensiveStat)
        {
            ActionId = actionId;
            _entityStats = ctx.EntityStats;
            _offensiveStat = offensiveStat;
            _defensiveStat = defensiveStat;
        }

        public void Execute(ActionContext context)
        {
            var offense = _entityStats.GetStat(context.Source, _offensiveStat);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var defense = _entityStats.GetStat(target, _defensiveStat);
                var damage = StatScaling.OffensiveScale(context.Value, offense, defense);
                _entityStats.ApplyDamage(target, damage, context.Source);
            }
        }
    }
}
