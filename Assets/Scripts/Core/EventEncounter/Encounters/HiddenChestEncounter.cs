using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class HiddenChestEncounter : IEventEncounterProvider
    {
        public string EncounterId => "test_chest";

        public EventEncounterDefinition CreateDefinition() => new("test_chest", "Hidden Chest", new[]
        {
            new InteractableDefinition("Chest", 20, new Color(0.6f, 0.4f, 0.2f), new[]
            {
                new InteractionReaction("Open", "consume", null, 0),
                new InteractionReaction("Search", "message", "You notice the lock is rusted...", 0),
            },
            "A locked wooden chest",
            Tags: new[] { "flammable", "breakable", "meltable", "chest", "lockpickable" },
            DeathReward: "random",
            DeathRewardValue: 1),
        });
    }
}
