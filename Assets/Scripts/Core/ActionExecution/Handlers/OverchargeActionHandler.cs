using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class OverchargeActionHandler : IActionHandler
    {
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private int _nextId;

        public string ActionId => ActionNames.Overcharge;

        public OverchargeActionHandler(IActionHandlerContext ctx)
        {
            _entityStats = ctx.EntityStats;
            _statusEffects = ctx.StatusEffects;
        }

        public void Execute(ActionContext context)
        {
            var buffId = $"overcharge_mgc_{_nextId++}";
            _entityStats.AddModifier(context.Source, StatType.MagicPower,
                new StatBuffModifier(buffId, context.Value));

            _statusEffects.ApplyEffect(context.Source, StatusEffectType.Energetic,
                context.Value, context.Source);
        }
    }
}
