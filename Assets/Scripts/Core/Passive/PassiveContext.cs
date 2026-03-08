using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveContext : IPassiveContext
    {
        public IEntityStatsService EntityStats { get; }
        public ICombatSlotService SlotService { get; }
        public IEventBus EventBus { get; }
        public IEncounterService EncounterService { get; }

        public PassiveContext(
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            IEventBus eventBus,
            IEncounterService encounterService)
        {
            EntityStats = entityStats;
            SlotService = slotService;
            EventBus = eventBus;
            EncounterService = encounterService;
        }
    }
}
