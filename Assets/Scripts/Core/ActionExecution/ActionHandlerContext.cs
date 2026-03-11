using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class ActionHandlerContext : IActionHandlerContext
    {
        public IEntityStatsService EntityStats { get; }
        public IEventBus EventBus { get; }
        public ICombatContext CombatContext { get; }
        public IStatusEffectService StatusEffects { get; set; }
        public ITurnService TurnService { get; }
        public IWeaponService WeaponService { get; }
        public StatusEffectInteractionTable InteractionTable { get; }
        public IReadOnlyDictionary<string, EntityDefinition> UnitRegistry { get; }
        public ICombatSlotService SlotService { get; }
        public IGameServices Services { get; }
        public IEntityTagProvider EntityTagProvider { get; }

        public ActionHandlerContext(
            IEntityStatsService entityStats,
            IEventBus eventBus,
            ICombatContext combatContext,
            IStatusEffectService statusEffects = null,
            ITurnService turnService = null,
            IWeaponService weaponService = null,
            StatusEffectInteractionTable interactionTable = null,
            IReadOnlyDictionary<string, EntityDefinition> unitRegistry = null,
            ICombatSlotService slotService = null,
            IEntityTagProvider entityTagProvider = null)
        {
            EntityStats = entityStats;
            EventBus = eventBus;
            CombatContext = combatContext;
            StatusEffects = statusEffects;
            TurnService = turnService;
            WeaponService = weaponService;
            InteractionTable = interactionTable ?? new StatusEffectInteractionTable();
            UnitRegistry = unitRegistry;
            SlotService = slotService;
            EntityTagProvider = entityTagProvider;
        }

        public ActionHandlerContext(IGameServices services, ICombatContext combatContext)
        {
            Services = services;
            EntityStats = services.EntityStats;
            EventBus = services.EventBus;
            CombatContext = combatContext;
            StatusEffects = services.StatusEffects;
            TurnService = services.TurnService;
            WeaponService = services.WeaponService;
            InteractionTable = services.InteractionTable ?? new StatusEffectInteractionTable();
            UnitRegistry = services.UnitRegistry;
            SlotService = services.SlotService;
        }
    }
}
