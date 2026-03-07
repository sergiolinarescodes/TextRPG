using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
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

        public ActionHandlerContext(
            IEntityStatsService entityStats,
            IEventBus eventBus,
            ICombatContext combatContext,
            IStatusEffectService statusEffects = null,
            ITurnService turnService = null,
            IWeaponService weaponService = null,
            StatusEffectInteractionTable interactionTable = null)
        {
            EntityStats = entityStats;
            EventBus = eventBus;
            CombatContext = combatContext;
            StatusEffects = statusEffects;
            TurnService = turnService;
            WeaponService = weaponService;
            InteractionTable = interactionTable ?? new StatusEffectInteractionTable();
        }
    }
}
