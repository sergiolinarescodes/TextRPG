using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Reactions
{
    internal sealed class ReactionService : SystemServiceBase, IEntityTagProvider
    {
        private readonly Dictionary<EntityId, List<InteractionReaction>> _reactions = new();
        private readonly Dictionary<EntityId, string[]> _entityTags = new();
        private readonly InteractionOutcomeRegistry _outcomes;
        private readonly IEventEncounterContext _ctx;
        private readonly TagReactionRegistry _tagReactions;
        private readonly ICombatContext _combatContext;
        private bool _isProcessing;

        public ReactionService(
            IEventBus eventBus,
            InteractionOutcomeRegistry outcomes,
            IEventEncounterContext ctx,
            TagReactionRegistry tagReactions = null,
            ICombatContext combatContext = null) : base(eventBus)
        {
            _outcomes = outcomes;
            _ctx = ctx;
            _tagReactions = tagReactions;
            _combatContext = combatContext;

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

        public string[] GetEntityTags(EntityId entityId)
            => _entityTags.TryGetValue(entityId, out var tags) ? tags : null;

        public void ClearReactions()
        {
            _reactions.Clear();
            _entityTags.Clear();
            _tagReactions?.ClearAllState();
        }

        public void ClearReactions(EntityId entityId)
        {
            _reactions.Remove(entityId);
            _entityTags.Remove(entityId);
            _tagReactions?.ClearEntityState(entityId);
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

            bool isGive = _combatContext?.IsGiveCommand == true;

            _isProcessing = true;
            try
            {
                if (hasEntityReactions)
                {
                    if (isGive)
                    {
                        // First affordable match wins
                        for (int i = 0; i < reactions.Count; i++)
                        {
                            if (TryExecuteGiveReaction(reactions[i], source, target, actionId, value))
                                break;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < reactions.Count; i++)
                            ExecuteReaction(reactions[i], source, target, actionId, value);
                    }
                }

                if (hasTagReactions)
                {
                    for (int t = 0; t < tags.Length; t++)
                    {
                        if (_tagReactions.TryGet(tags[t], out var tagDef))
                        {
                            var tagCtx = new TagReactionContext(
                                source, target, actionId, value,
                                _ctx, _tagReactions.StateStore, tagDef.TagId);
                            try { tagDef.React(tagCtx); }
                            catch (Exception ex) { Debug.LogException(ex); }
                        }
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private bool TryExecuteGiveReaction(InteractionReaction reaction, EntityId source,
            EntityId target, string actionId, int value)
        {
            if (!string.Equals(reaction.ActionId, actionId, StringComparison.OrdinalIgnoreCase))
                return false;

            // Value check: if reaction has a cost, verify payment meets cost
            if (reaction.Value > 0 && value > 0 && value < reaction.Value)
                return false;

            if (reaction.Chance < 1.0f && UnityEngine.Random.value > reaction.Chance)
                return false;

            if (!_outcomes.TryGet(reaction.OutcomeId, out var outcome))
                return false;

            var outcomeValue = reaction.Value != 0 ? reaction.Value : value;
            var context = new InteractionOutcomeContext(source, target, actionId, outcomeValue, reaction.OutcomeParam, _ctx);
            outcome.Execute(context);
            return true;
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
