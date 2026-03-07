using TextRPG.Core.EntityStats;
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
    }
}
