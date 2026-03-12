using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct ActionExecutionStartedEvent(string Word, int ActionCount);
    public readonly record struct ActionHandlerExecutedEvent(string ActionId, int Value, EntityId Source, IReadOnlyList<EntityId> Targets);
    public readonly record struct ActionExecutionCompletedEvent(string Word);
    public readonly record struct PushActionEvent(EntityId Source, EntityId Target, int Value);
    public readonly record struct WordRejectedEvent(string Word, int ManaCost);
    public readonly record struct UnitSummonedEvent(
        EntityId EntityId, EntityId Owner, CombatSlot.CombatSlot Slot,
        string UnitType = "enemy", string Word = "");

    public readonly record struct StatSiphonedEvent(EntityId Source, EntityId Target, int Amount);

    public readonly record struct ResolvedAction(
        string ActionId, int Value, EntityId Source,
        IReadOnlyList<EntityId> Targets, string Word, string AssocWord = "", bool IsCritical = false);

    public readonly record struct ActionResolvedEvent(
        string Word, IReadOnlyList<ResolvedAction> Actions,
        EntityId Source, bool IsInstant);
}
