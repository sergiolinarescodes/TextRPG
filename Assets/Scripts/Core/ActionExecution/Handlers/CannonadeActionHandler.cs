using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class CannonadeActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ICombatContext _combatContext;
        private readonly ILuckService _luckService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Cannonade;

        public CannonadeActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _combatContext = ctx.CombatContext;
            _luckService = ctx.LuckService;
            _eventBus = ctx.EventBus;
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

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage);
            }
        }
    }
}
