using TextRPG.Core.Encounter;
using UnityEngine;

namespace TextRPG.Core.EventEncounter
{
    public sealed record EventEncounterDefinition(
        string Id,
        string DisplayName,
        InteractableDefinition[] Interactables
    );

    public sealed record InteractableDefinition(
        string Name,
        int MaxHealth,
        Color Color,
        InteractionReaction[] Reactions,
        string Description = null,
        string[] Tags = null,
        string DeathReward = null,
        int DeathRewardValue = 0,
        PassiveEntry[] Passives = null
    );

    public sealed record InteractionReaction(
        string ActionId,
        string OutcomeId,
        string OutcomeParam,
        int Value,
        float Chance = 1.0f
    );
}
