using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class ReactionService : SystemServiceBase
    {
        private readonly Dictionary<EntityId, List<InteractionReaction>> _reactions = new();
        private readonly Dictionary<EntityId, string[]> _entityTags = new();
        private readonly InteractionOutcomeRegistry _outcomes;
        private readonly IEventEncounterContext _ctx;
        private readonly TagReactionRegistry _tagReactions;
        private bool _isProcessing;

        public ReactionService(
            IEventBus eventBus,
            InteractionOutcomeRegistry outcomes,
            IEventEncounterContext ctx,
            TagReactionRegistry tagReactions = null) : base(eventBus)
        {
            _outcomes = outcomes;
            _ctx = ctx;
            _tagReactions = tagReactions;

            Subscribe<InteractionActionEvent>(OnInteractionAction);
            Subscribe<ActionHandlerExecutedEvent>(OnCombatAction);
        }

        public void RegisterReactions(EntityId entityId, InteractionReaction[] reactions, string[] tags = null)
        {
            if (reactions != null && reactions.Length > 0)
                _reactions[entityId] = new List<InteractionReaction>(reactions);

            if (tags != null && tags.Length > 0)
                _entityTags[entityId] = tags;
        }

        public void ClearReactions()
        {
            _reactions.Clear();
            _entityTags.Clear();
        }

        public void ClearReactions(EntityId entityId)
        {
            _reactions.Remove(entityId);
            _entityTags.Remove(entityId);
        }

        private void OnInteractionAction(InteractionActionEvent evt)
        {
            for (int i = 0; i < evt.Targets.Count; i++)
                ProcessReactions(evt.Source, evt.Targets[i], evt.ActionId, evt.Value);
        }

        private void OnCombatAction(ActionHandlerExecutedEvent evt)
        {
            for (int i = 0; i < evt.Targets.Count; i++)
                ProcessReactions(evt.Source, evt.Targets[i], evt.ActionId, evt.Value);
        }

        private void ProcessReactions(EntityId source, EntityId target, string actionId, int value)
        {
            if (_isProcessing) return;

            bool hasEntityReactions = _reactions.TryGetValue(target, out var reactions);
            _entityTags.TryGetValue(target, out var tags);
            bool hasTagReactions = _tagReactions != null && tags != null;

            if (!hasEntityReactions && !hasTagReactions) return;

            _isProcessing = true;
            try
            {
                if (hasEntityReactions)
                {
                    for (int i = 0; i < reactions.Count; i++)
                        ExecuteReaction(reactions[i], source, target, actionId, value);
                }

                if (hasTagReactions)
                {
                    for (int t = 0; t < tags.Length; t++)
                    {
                        var tagList = _tagReactions.GetReactions(tags[t], actionId);
                        for (int i = 0; i < tagList.Count; i++)
                            ExecuteReaction(tagList[i], source, target, actionId, value);
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void ExecuteReaction(InteractionReaction reaction, EntityId source, EntityId target, string actionId, int value)
        {
            if (!string.Equals(reaction.ActionId, actionId, StringComparison.OrdinalIgnoreCase))
                return;

            if (reaction.Chance < 1.0f && UnityEngine.Random.value > reaction.Chance)
                return;

            if (!_outcomes.TryGet(reaction.OutcomeId, out var outcome))
            {
                Debug.LogWarning($"[ReactionService] Unknown outcome: {reaction.OutcomeId}");
                return;
            }

            var outcomeValue = reaction.Value != 0 ? reaction.Value : value;
            var context = new InteractionOutcomeContext(
                source, target, actionId, outcomeValue, reaction.OutcomeParam, _ctx);

            try
            {
                outcome.Execute(context);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
