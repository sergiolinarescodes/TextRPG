using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct TargetingPreview(
        IReadOnlyList<ActionTargetPreview> ActionPreviews);

    public readonly record struct ActionTargetPreview(
        string ActionId,
        IReadOnlyList<EntityId> AffectedEntities);
}
