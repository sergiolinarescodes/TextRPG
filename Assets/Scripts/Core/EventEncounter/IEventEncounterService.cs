using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EventEncounter
{
    public interface IEventEncounterService
    {
        void StartEncounter(EventEncounterDefinition encounter, EntityId player);
        void EndEncounter();
        bool IsEncounterActive { get; }
        IReadOnlyList<EntityId> InteractableEntities { get; }
        InteractableDefinition GetDefinition(EntityId entityId);
        EntityDefinition GetEntityDefinition(EntityId entityId);
        EntityId PlayerEntity { get; }
    }
}
