using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EventEncounter.Reactions
{
    public readonly record struct InteractionOutcomeContext(
        EntityId Source,
        EntityId Target,
        string ActionId,
        int Value,
        string OutcomeParam,
        IEventEncounterContext Ctx
    );
}
