using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;

namespace TextRPG.Core.EventEncounter.Reactions
{
    public interface IEventEncounterContext
    {
        IEntityStatsService EntityStats { get; }
        ICombatSlotService SlotService { get; }
        IEventBus EventBus { get; }
        IStatusEffectService StatusEffects { get; }
        IEventEncounterService EncounterService { get; }
        IResourceService ResourceService { get; }
        IInventoryService InventoryService { get; }
        InventoryId PlayerInventoryId { get; }
        IItemRegistry ItemRegistry { get; }
    }
}
