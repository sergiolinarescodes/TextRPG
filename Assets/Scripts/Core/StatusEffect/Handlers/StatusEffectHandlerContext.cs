using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;

namespace TextRPG.Core.StatusEffect.Handlers
{
    internal sealed class StatusEffectHandlerContext : IStatusEffectHandlerContext
    {
        public IEntityStatsService EntityStats { get; }
        public ITurnService TurnService { get; }
        public IStatusEffectService StatusEffects { get; set; }
        public IEventBus EventBus { get; }
        public IEncounterService EncounterService { get; set; }

        public StatusEffectHandlerContext(IEntityStatsService entityStats, ITurnService turnService, IEventBus eventBus)
        {
            EntityStats = entityStats;
            TurnService = turnService;
            EventBus = eventBus;
        }
    }
}
