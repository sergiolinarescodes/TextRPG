using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;

namespace TextRPG.Core.StatusEffect.Handlers
{
    public interface IStatusEffectHandlerContext
    {
        IEntityStatsService EntityStats { get; }
        ITurnService TurnService { get; }
        IStatusEffectService StatusEffects { get; }
        IEventBus EventBus { get; }
        IEncounterService EncounterService { get; }
        ILuckService LuckService { get; }
    }
}
