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
    public interface IPassiveContext
    {
        IEntityStatsService EntityStats { get; }
        ICombatSlotService SlotService { get; }
        IEventBus EventBus { get; }
        IEncounterService EncounterService { get; }
        IStatusEffectService StatusEffects { get; }
        IWordTagResolver TagResolver { get; }
        ITurnService TurnService { get; }
        IActionAnimationService AnimationService { get; }
        IResourceService ResourceService { get; }
        ILetterChallengeService LetterChallengeService { get; }
    }
}
