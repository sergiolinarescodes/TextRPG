using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class CleaveActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly ICombatContext _combatContext;
        private readonly ILuckService _luckService;
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Cleave;

        public CleaveActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _combatContext = ctx.CombatContext;
            _luckService = ctx.LuckService;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            var offense = _entityStats.GetStat(context.Source, StatType.Strength);

            // Primary target(s): full damage
            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var defense = _entityStats.GetStat(target, StatType.PhysicalDefense);
                var damage = StatScaling.OffensiveScale(context.Value, offense, defense);

                damage = StatScaling.ApplyCritical(damage, context, _luckService, _eventBus, target);
                _entityStats.ApplyDamage(target, damage, context.Source);
            }

            // Splash: pick one other random enemy not in primary targets, deal half damage
            var allEnemies = _combatContext.GetTargets(TargetType.AllEnemies);
            var candidates = new List<EntityId>();
            for (int i = 0; i < allEnemies.Count; i++)
            {
                var enemy = allEnemies[i];
                if (_entityStats.GetCurrentHealth(enemy) <= 0) continue;
                bool isPrimary = false;
                for (int j = 0; j < context.Targets.Count; j++)
                {
                    if (context.Targets[j].Equals(enemy)) { isPrimary = true; break; }
                }
                if (!isPrimary) candidates.Add(enemy);
            }

            if (candidates.Count > 0)
            {
                var splash = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                var splashDef = _entityStats.GetStat(splash, StatType.PhysicalDefense);
                int halfValue = (context.Value + 1) / 2;
                var splashDmg = StatScaling.OffensiveScale(halfValue, offense, splashDef);

                splashDmg = StatScaling.ApplyCritical(splashDmg, context, _luckService, _eventBus, splash);
                _entityStats.ApplyDamage(splash, splashDmg, context.Source);
            }
        }
    }
}
