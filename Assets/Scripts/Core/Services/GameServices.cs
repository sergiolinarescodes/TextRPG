using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.Luck;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;

namespace TextRPG.Core.Services
{
    internal sealed class GameServices : IGameServices
    {
        public IEventBus EventBus { get; set; }
        public IEntityStatsService EntityStats { get; set; }
        public ICombatSlotService SlotService { get; set; }
        public IStatusEffectService StatusEffects { get; set; }
        public ITurnService TurnService { get; set; }
        public IEncounterService EncounterService { get; set; }
        public ICombatContext CombatContext { get; set; }
        public IWeaponService WeaponService { get; set; }
        public IActionAnimationService AnimationService { get; set; }
        public IWordTagResolver TagResolver { get; set; }
        public IResourceService ResourceService { get; set; }
        public IInventoryService InventoryService { get; set; }
        public IItemRegistry ItemRegistry { get; set; }
        public StatusEffectInteractionTable InteractionTable { get; set; }
        public IReadOnlyDictionary<string, EntityDefinition> UnitRegistry { get; set; }
        public ILuckService LuckService { get; set; }
    }
}
