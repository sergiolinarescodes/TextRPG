using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class RoadsideInnEncounter : IEventEncounterProvider
    {
        public string EncounterId => "test_inn";

        public EventEncounterDefinition CreateDefinition() => new("test_inn", "Roadside Inn", new[]
        {
            new InteractableDefinition("Innkeeper", 99, new Color(0.9f, 0.7f, 0.3f), new[]
            {
                new InteractionReaction("Talk", "message", "Welcome, traveler!", 0),
                new InteractionReaction("Rest", "heal", null, 20),
                new InteractionReaction("Trade", "message", "I have wares if you have coin.", 0),
            }, "A friendly innkeeper"),
        });
    }
}
