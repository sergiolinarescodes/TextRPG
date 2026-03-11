using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class EventEncounterContext : IEventEncounterContext
    {
        public IEntityStatsService EntityStats { get; }
        public ICombatSlotService SlotService { get; }
        public IEventBus EventBus { get; }
        public IStatusEffectService StatusEffects { get; }
        public IEventEncounterService EncounterService { get; set; }
        public IResourceService ResourceService { get; }
        public IInventoryService InventoryService { get; }
        public InventoryId PlayerInventoryId { get; }
        public IItemRegistry ItemRegistry { get; }

        public EventEncounterContext(
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            IEventBus eventBus,
            IStatusEffectService statusEffects,
            IResourceService resourceService = null,
            IInventoryService inventoryService = null,
            InventoryId playerInventoryId = default,
            IItemRegistry itemRegistry = null)
        {
            EntityStats = entityStats;
            SlotService = slotService;
            EventBus = eventBus;
            StatusEffects = statusEffects;
            ResourceService = resourceService;
            InventoryService = inventoryService;
            PlayerInventoryId = playerInventoryId;
            ItemRegistry = itemRegistry;
        }
    }
}
