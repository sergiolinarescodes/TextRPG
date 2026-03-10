using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class MagicDamageActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => ActionNames.MagicDamage;

        public MagicDamageActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            var sourceMagic = _entityStats.GetStat(context.Source, StatType.MagicPower);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var targetMDef = _entityStats.GetStat(target, StatType.MagicDefense);
                var damage = StatScaling.OffensiveScale(context.Value, sourceMagic, targetMDef);
                _entityStats.ApplyDamage(target, damage, context.Source);
            }
        }
    }
}
