using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.LetterReserve;
using TextRPG.Core.Services;
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
        IReadOnlyDictionary<string, EntityDefinition> UnitRegistry { get; }
        ICombatSlotService SlotService { get; }
        IGameServices Services { get; }
        IEntityTagProvider EntityTagProvider { get; }
        ILetterReserveService LetterReserve { get; }
    }
}
