using Unidad.Core.Grid;

namespace TextRPG.Core.Encounter
{
    public sealed record EncounterDefinition(
        string Id,
        string DisplayName,
        EnemyDefinition[] Enemies,
        GridPosition[] EnemyPositions
    );
}
