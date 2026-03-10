using TextRPG.Core.Encounter;
using TextRPG.Core.EventEncounter;

namespace TextRPG.Core.Run
{
    public sealed record RunNode(
        int Index,
        RunNodeType NodeType,
        EncounterDefinition CombatEncounter,
        EventEncounterDefinition EventEncounter
    );

    public sealed record RunDefinition(string Id, RunNode[] Nodes);
}
