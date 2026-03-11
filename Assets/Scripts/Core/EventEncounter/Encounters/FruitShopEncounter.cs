using TextRPG.Core.EventEncounter.Reactions.Outcomes;
using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class FruitShopEncounter : IEventEncounterProvider
    {
        public string EncounterId => "fruit_shop";

        public EventEncounterDefinition CreateDefinition() => new(
            "fruit_shop", "Fruit Stand",
            new[]
            {
                new InteractableDefinition(
                    "Vendor", 99, new Color(0.4f, 0.8f, 0.3f),
                    new[]
                    {
                        new InteractionReaction("Talk", "message", "Fresh fruit! Pay what you will.", 0),
                        new InteractionReaction("Pay", "give_item", GiveItemOutcome.RandomFruit, 0),
                    },
                    Description: "A cheerful fruit vendor. Any gold gets you a random fruit.")
            });
    }
}
