namespace TextRPG.Core.Encounter
{
    public sealed record EncounterDefinition(
        string Id,
        string DisplayName,
        EnemyDefinition[] Enemies
    );
}
