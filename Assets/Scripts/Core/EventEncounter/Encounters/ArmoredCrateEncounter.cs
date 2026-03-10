using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class ArmoredCrateEncounter : IEventEncounterProvider
    {
        public string EncounterId => "test_armored_crate";

        public EventEncounterDefinition CreateDefinition() => new("test_armored_crate", "Armored Crate", new[]
        {
            new InteractableDefinition("Crate", 60, new Color(0.4f, 0.4f, 0.45f), new[]
            {
                new InteractionReaction("Open", "message", "Reinforced plating resists your attempt.", 0),
                new InteractionReaction("Search", "message", "Steel plates riveted over thick wood. Joints look weldable.", 0),
            },
            "A steel-plated military crate",
            Tags: new[] { "meltable", "conductive", "breakable" },
            DeathReward: "random",
            DeathRewardValue: 4),
        });
    }
}
