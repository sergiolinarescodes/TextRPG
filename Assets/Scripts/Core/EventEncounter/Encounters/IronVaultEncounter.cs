using UnityEngine;

namespace TextRPG.Core.EventEncounter.Encounters
{
    internal sealed class IronVaultEncounter : IEventEncounterProvider
    {
        public string EncounterId => "test_iron_vault";

        public EventEncounterDefinition CreateDefinition() => new("test_iron_vault", "Iron Vault", new[]
        {
            new InteractableDefinition("Vault", 40, new Color(0.5f, 0.5f, 0.6f), new[]
            {
                new InteractionReaction("Open", "message", "It's sealed shut with heavy bolts.", 0),
                new InteractionReaction("Search", "message", "You see scorch marks around the lock...", 0),
            },
            "A heavy iron vault",
            Tags: new[] { "meltable", "conductive" },
            DeathReward: "random",
            DeathRewardValue: 3),
        });
    }
}
