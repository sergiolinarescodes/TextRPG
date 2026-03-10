namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class DatabaseEncounterProvider : IEventEncounterProvider
    {
        private readonly EventEncounterDefinition _definition;

        public string EncounterId => _definition.Id;

        public DatabaseEncounterProvider(EventEncounterDefinition definition)
        {
            _definition = definition;
        }

        public EventEncounterDefinition CreateDefinition() => _definition;
    }
}
