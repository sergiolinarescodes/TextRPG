using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive
{
    public interface IPassiveContext
    {
        IEntityStatsService EntityStats { get; }
        ICombatSlotService SlotService { get; }
        IEventBus EventBus { get; }
        IEncounterService EncounterService { get; }
    }
}
