using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class EventEncounterContext : IEventEncounterContext
    {
        public IEntityStatsService EntityStats { get; }
        public ICombatSlotService SlotService { get; }
        public IEventBus EventBus { get; }
        public IStatusEffectService StatusEffects { get; }
        public IEventEncounterService EncounterService { get; set; }

        public EventEncounterContext(
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            IEventBus eventBus,
            IStatusEffectService statusEffects)
        {
            EntityStats = entityStats;
            SlotService = slotService;
            EventBus = eventBus;
            StatusEffects = statusEffects;
        }
    }
}
