using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct ActionExecutionStartedEvent(string Word, int ActionCount);
    public readonly record struct ActionHandlerExecutedEvent(string ActionId, int Value, EntityId Source, IReadOnlyList<EntityId> Targets);
    public readonly record struct ActionExecutionCompletedEvent(string Word);
    public readonly record struct PushActionEvent(EntityId Source, EntityId Target, int Value);
    public readonly record struct FireGridStatusEvent(EntityId Source, int Duration);
    public readonly record struct MovementActionEvent(EntityId Entity, GridPosition From, GridPosition To, string MovementType, bool IsTeleport);
}
