using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class DamageActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;

        public string ActionId => "Damage";

        public DamageActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
        }

        public void Execute(ActionContext context)
        {
            var sourceStrength = _entityStats.GetStat(context.Source, StatType.Strength);

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var targetDefense = _entityStats.GetStat(target, StatType.PhysicalDefense);
                var damage = Math.Max(1, context.Value * sourceStrength / Math.Max(1, targetDefense));
                _entityStats.ApplyDamage(target, damage, context.Source);
            }
        }
    }
}
