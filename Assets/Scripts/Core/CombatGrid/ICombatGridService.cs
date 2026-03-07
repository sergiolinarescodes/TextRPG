using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.Grid;

namespace TextRPG.Core.CombatGrid
{
    public interface ICombatGridService
    {
        IGrid<UnitId?> Grid { get; }
        void Initialize(int width, int height);
        void RegisterCombatant(EntityId entityId, UnitDefinition unitDef, GridPosition position);
        void RemoveCombatant(EntityId entityId);
        GridPosition GetPosition(EntityId entityId);
        EntityId? GetEntityAt(GridPosition position);
        IReadOnlyList<EntityId> GetEntitiesInRange(GridPosition center, int range);
        IReadOnlyList<EntityId> GetAdjacentEntities(GridPosition position);
        int GetDistance(EntityId a, EntityId b);
        bool CanMoveTo(GridPosition position);
        void MoveEntity(EntityId entityId, GridPosition to);
    }
}
