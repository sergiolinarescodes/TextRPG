using System.Collections.Generic;
using Unidad.Core.Grid;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct TargetingPreview(
        IReadOnlyList<ActionTargetPreview> ActionPreviews);

    public readonly record struct ActionTargetPreview(
        string ActionId,
        IReadOnlyList<GridPosition> AffectedPositions);
}
