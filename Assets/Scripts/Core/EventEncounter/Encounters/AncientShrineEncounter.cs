using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class AncientShrineEncounter : IEventEncounterProvider
    {
        public string EncounterId => "test_shrine";

        public EventEncounterDefinition CreateDefinition() => new("test_shrine", "Ancient Shrine", new[]
        {
            new InteractableDefinition("Shrine", 50, new Color(0.5f, 0.7f, 1f), new[]
            {
                new InteractionReaction("Pray", "heal", null, 30),
                new InteractionReaction("Pray", "mana", null, 10),
                new InteractionReaction("Enter", "message", "The shrine glows with divine light.", 0),
            }, "A glowing shrine"),
        });
    }
}
