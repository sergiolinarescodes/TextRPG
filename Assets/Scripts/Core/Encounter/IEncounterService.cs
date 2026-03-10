using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Encounter
{
    public interface IEncounterService
    {
        void StartEncounter(EncounterDefinition encounter, EntityId player);
        void EndEncounter();
        bool IsEncounterActive { get; }
        IReadOnlyList<EntityId> EnemyEntities { get; }
        bool IsEnemy(EntityId entityId);
        EntityDefinition GetEntityDefinition(EntityId entityId);
        void RegisterEnemy(EntityId entityId, EntityDefinition definition = null);
        EntityId PlayerEntity { get; }
    }
}
