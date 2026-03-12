using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Lockpick
{
    public readonly record struct LockpickAttemptEvent(
        EntityId Source, IReadOnlyList<EntityId> Targets);

    public readonly record struct LockpickCompletedEvent(
        EntityId Source, EntityId Target, bool Success);
}
