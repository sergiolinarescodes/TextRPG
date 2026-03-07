using Unidad.Core.Grid;

namespace TextRPG.Core.UnitRendering
{
    public interface IUnitService
    {
        UnitInstance PlaceUnit(UnitDefinition definition, GridPosition position, IGrid<UnitId?> grid);
        void RemoveUnit(UnitId id, IGrid<UnitId?> grid);
        void MoveUnit(UnitId id, GridPosition to, IGrid<UnitId?> grid);
        UnitInstance GetUnit(UnitId id);
        bool TryGetUnit(UnitId id, out UnitInstance instance);
        bool HasUnitAt(GridPosition position, IGrid<UnitId?> grid);
        UnitInstance GetUnitAt(GridPosition position, IGrid<UnitId?> grid);
    }
}
