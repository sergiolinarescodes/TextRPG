using Unidad.Core.Grid;

namespace TextRPG.Core.UnitRendering
{
    public readonly record struct UnitPlacedEvent(UnitId UnitId, GridPosition Position);
    public readonly record struct UnitRemovedEvent(UnitId UnitId, GridPosition Position);
    public readonly record struct UnitMovedEvent(UnitId UnitId, GridPosition From, GridPosition To);
}
