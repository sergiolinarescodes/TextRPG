using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class TavernEncounter : IEventEncounterProvider
    {
        public string EncounterId => "tavern";

        public EventEncounterDefinition CreateDefinition() => new(
            "tavern", "Tavern",
            new[]
            {
                new InteractableDefinition(
                    "Bartender", 99, new Color(0.8f, 0.6f, 0.3f),
                    new[]
                    {
                        new InteractionReaction("Talk", "message", "What'll it be?", 0),
                        // Ordered cost descending — first affordable match wins for "give"
                        new InteractionReaction("Pay", "give_item", "pot", 10),
                        new InteractionReaction("Pay", "give_item", "beer", 5),
                    },
                    Description: "A tavern keeper. Pay gold for food and drink.")
            });
    }
}
