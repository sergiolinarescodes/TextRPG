using TextRPG.Core.ActionAnimation;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.LetterChallenge;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Resource;

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
        public IResourceService ResourceService { get; }
        public ILetterChallengeService LetterChallengeService { get; }

        public PassiveContext(
            IEntityStatsService entityStats,
            ICombatSlotService slotService,
            IEventBus eventBus,
            IEncounterService encounterService,
            IStatusEffectService statusEffects = null,
            IWordTagResolver tagResolver = null,
            ITurnService turnService = null,
            IActionAnimationService animationService = null,
            IResourceService resourceService = null,
            ILetterChallengeService letterChallengeService = null)
        {
            EntityStats = entityStats;
            SlotService = slotService;
            EventBus = eventBus;
            EncounterService = encounterService;
            StatusEffects = statusEffects;
            TagResolver = tagResolver;
            TurnService = turnService;
            AnimationService = animationService;
            ResourceService = resourceService;
            LetterChallengeService = letterChallengeService;
        }
    }
}
