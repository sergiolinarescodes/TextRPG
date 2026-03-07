using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;

namespace TextRPG.Core.Encounter
{
    public interface IEncounterService
    {
        void StartEncounter(EncounterDefinition encounter, EntityId player, GridPosition playerPosition);
        void EndEncounter();
        bool IsEncounterActive { get; }
        IReadOnlyList<EntityId> EnemyEntities { get; }
        bool IsEnemy(EntityId entityId);
        EnemyDefinition GetEnemyDefinition(EntityId entityId);
    }
}
