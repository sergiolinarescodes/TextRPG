using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class ShockActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly ICombatContext _combatContext;
        private readonly StatusEffectInteractionTable _interactionTable;

        public string ActionId => "Shock";

        public ShockActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
            _combatContext = ctx.CombatContext;
            _interactionTable = ctx.InteractionTable;
        }

        public void Execute(ActionContext context)
        {
            var grid = _combatContext.CombatGrid;

            for (int i = 0; i < context.Targets.Count; i++)
            {
                var target = context.Targets[i];
                var damage = (int)(context.Value * _interactionTable.GetDamageMultiplier("Shock", target, _statusEffects));

                _entityStats.ApplyDamage(target, damage);

                if (grid != null)
                {
                    var targetPos = grid.GetPosition(target);
                    var adjacent = grid.GetAdjacentEntities(targetPos);
                    foreach (var secondary in adjacent)
                    {
                        if (secondary.Equals(context.Source))
                            continue;

                        var secDamage = (int)(context.Value * _interactionTable.GetDamageMultiplier("Shock", secondary, _statusEffects));

                        _entityStats.ApplyDamage(secondary, secDamage);
                    }
                }
            }
        }
    }
}
