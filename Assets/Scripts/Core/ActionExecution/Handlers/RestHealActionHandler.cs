using System;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class RestHealActionHandler : IActionHandler
    {
        private readonly IActionHandlerContext _ctx;

        public string ActionId => "RestHeal";

        public RestHealActionHandler(IActionHandlerContext ctx)
        {
            _ctx = ctx;
        }

        public void Execute(ActionContext context)
        {
            int heal = context.Value;

            // Bonus if no enemies present
            var enemies = _ctx.CombatContext.GetTargets(TargetType.AllEnemies, 0);
            if (enemies.Count == 0)
                heal += 5;

            // Bonus if any slot entity has DWELLING tag
            if (_ctx.EntityTagProvider != null)
            {
                var allEntities = _ctx.CombatContext.GetTargets(TargetType.All, 0);
                for (int i = 0; i < allEntities.Count; i++)
                {
                    var tags = _ctx.EntityTagProvider.GetEntityTags(allEntities[i]);
                    if (tags == null) continue;
                    for (int t = 0; t < tags.Length; t++)
                    {
                        if (string.Equals(tags[t], "DWELLING", StringComparison.OrdinalIgnoreCase))
                        {
                            heal += 10;
                            goto doneTagCheck;
                        }
                    }
                }
            }
            doneTagCheck:

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                int targetHeal = heal;

                // Scale with target's MagicPower
                if (_ctx.EntityStats != null)
                {
                    int magicPower = _ctx.EntityStats.GetStat(target, StatType.MagicPower);
                    targetHeal += magicPower / 3;
                }

                _ctx.EntityStats?.ApplyHeal(target, targetHeal);
            }
        }
    }
}
