using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;

namespace TextRPG.Core.CombatGrid
{
    public readonly record struct CombatGridInitializedEvent(int Width, int Height);
    public readonly record struct CombatantRegisteredEvent(EntityId EntityId, GridPosition Position);
    public readonly record struct CombatantRemovedEvent(EntityId EntityId, GridPosition Position);
    public readonly record struct CombatantMovedEvent(EntityId EntityId, GridPosition From, GridPosition To);
}
