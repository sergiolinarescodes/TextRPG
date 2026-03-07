using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution
{
    public interface IActionHandlerContext
    {
        IEntityStatsService EntityStats { get; }
        IEventBus EventBus { get; }
        ICombatContext CombatContext { get; }
        IStatusEffectService StatusEffects { get; }
        ITurnService TurnService { get; }
        IWeaponService WeaponService { get; }
        StatusEffectInteractionTable InteractionTable { get; }
    }
}
