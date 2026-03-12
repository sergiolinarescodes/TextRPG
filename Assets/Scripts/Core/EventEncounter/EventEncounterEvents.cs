using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EventEncounter
{
    public readonly record struct InteractionActionEvent(
        EntityId Source, string ActionId, IReadOnlyList<EntityId> Targets, int Value, string Word);

    public readonly record struct InteractionMessageEvent(string Message, EntityId SourceEntityId);

    public readonly record struct EventEncounterStartedEvent(string EncounterId, int InteractableCount);
    public readonly record struct EventEncounterEndedEvent(string EncounterId);
    public readonly record struct EventEncounterTransitionEvent(string TargetEncounterId);

    public readonly record struct RewardGrantedEvent(string RewardType, int Value);
    public readonly record struct SpawnCombatEvent(string EncounterId);
    public readonly record struct RecruitEvent(string UnitType);
    public readonly record struct EntityRecruitedEvent(EntityId EntityId, EntityId Recruiter);

    public readonly record struct ChestOpenedEvent(EntityId Chest, EntityId Opener);
}
