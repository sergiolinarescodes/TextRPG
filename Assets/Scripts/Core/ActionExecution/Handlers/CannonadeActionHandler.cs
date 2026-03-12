using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class CannonadeActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ICombatContext _combatContext;

        public string ActionId => ActionNames.Cannonade;

        public CannonadeActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _combatContext = ctx.CombatContext;
        }

        public void Execute(ActionContext context)
        {
            if (context.Targets.Count == 0) return;

            var sourceStr = _entityStats.GetStat(context.Source, StatType.Strength);
            int hitCount = Math.Max(1, context.Value);

            for (int i = 0; i < hitCount; i++)
            {
                var target = context.Targets[UnityEngine.Random.Range(0, context.Targets.Count)];
                if (_entityStats.GetCurrentHealth(target) <= 0) continue;

                int damage = StatScaling.OffensiveScale(1, sourceStr,
                    _entityStats.GetStat(target, StatType.PhysicalDefense));
                _entityStats.ApplyDamage(target, damage);
            }
        }
    }
}
