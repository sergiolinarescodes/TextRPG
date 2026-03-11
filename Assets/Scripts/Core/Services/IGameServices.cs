using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Resource;

namespace TextRPG.Core.Services
{
    public interface IGameServices
    {
        IEventBus EventBus { get; }
        IEntityStatsService EntityStats { get; }
        ICombatSlotService SlotService { get; }
        IStatusEffectService StatusEffects { get; }
        ITurnService TurnService { get; }
        IEncounterService EncounterService { get; }
        ICombatContext CombatContext { get; }
        IWeaponService WeaponService { get; }
        IActionAnimationService AnimationService { get; }
        IWordTagResolver TagResolver { get; }
        IResourceService ResourceService { get; }
        IInventoryService InventoryService { get; }
        IItemRegistry ItemRegistry { get; }
        StatusEffectInteractionTable InteractionTable { get; }
        IReadOnlyDictionary<string, EntityDefinition> UnitRegistry { get; }
    }
}
