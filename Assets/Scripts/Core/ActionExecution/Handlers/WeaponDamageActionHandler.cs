using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class WeaponDamageActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => ActionNames.WeaponDamage;

        public WeaponDamageActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            var sourceDex = _entityStats.GetStat(context.Source, StatType.Dexterity);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var targetDefense = _entityStats.GetStat(target, StatType.PhysicalDefense);
                var damage = StatScaling.OffensiveScale(context.Value, sourceDex, targetDefense);
                _entityStats.ApplyDamage(target, damage, context.Source);
            }
        }
    }
}
