using TextRPG.Core.ActionAnimation;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;

namespace TextRPG.Core.Passive
{
    internal sealed class PassiveContext : IPassiveContext
    {
        public IEntityStatsService EntityStats { get; }
        public ICombatSlotService SlotService { get; }
        public IEventBus EventBus { get; }
        public IEncounterService EncounterService { get; }
        public IStatusEffectService StatusEffects { get; }
        public IWordTagResolver TagResolver { get; }
        public ITurnService TurnService { get; }
        public IActionAnimationService AnimationService { get; }

        public PassiveContext(
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            IEventBus eventBus,
            IEncounterService encounterService,
            IStatusEffectService statusEffects = null,
            IWordTagResolver tagResolver = null,
            ITurnService turnService = null,
            IActionAnimationService animationService = null)
        {
            EntityStats = entityStats;
            SlotService = slotService;
            EventBus = eventBus;
            EncounterService = encounterService;
            StatusEffects = statusEffects;
            TagResolver = tagResolver;
            TurnService = turnService;
            AnimationService = animationService;
        }
    }
}
