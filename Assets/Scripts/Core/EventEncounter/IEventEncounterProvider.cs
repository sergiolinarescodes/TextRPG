namespace TextRPG.Core.EventEncounter
{
    public interface IEventEncounterProvider
    {
        string EncounterId { get; }
        EventEncounterDefinition CreateDefinition();
    }
}
