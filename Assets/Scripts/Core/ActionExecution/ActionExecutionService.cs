using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class ActionExecutionService : SystemServiceBase, IActionExecutionService
    {
        private readonly IWordResolver _wordResolver;
        private readonly IActionHandlerRegistry _handlerRegistry;
        private readonly ICombatContext _combatContext;
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly IAnimationResolver _animationResolver;

        public ActionExecutionService(IEventBus eventBus, IWordResolver wordResolver,
            IActionHandlerRegistry handlerRegistry, ICombatContext combatContext,
            IEntityStatsService entityStats = null, IStatusEffectService statusEffects = null,
            IAnimationResolver animationResolver = null)
            : base(eventBus)
        {
            _wordResolver = wordResolver;
            _handlerRegistry = handlerRegistry;
            _combatContext = combatContext;
            _entityStats = entityStats;
            _statusEffects = statusEffects;
            _animationResolver = animationResolver;
            Subscribe<WordSubmittedEvent>(OnWordSubmitted);
        }

        public void ExecuteWord(string word)
        {
            ProcessWord(word);
        }

        private void OnWordSubmitted(WordSubmittedEvent e)
        {
            ProcessWord(e.Word);
        }

        private void ProcessWord(string word)
        {
            if (!_wordResolver.HasWord(word))
                return;

            var actions = _wordResolver.Resolve(word);
            var meta = _wordResolver.GetStats(word);
            Publish(new WordResolvedEvent(word, actions, meta));

            if (_entityStats != null && meta.Cost > 0)
            {
                if (!_entityStats.TrySpendMana(_combatContext.SourceEntity, meta.Cost))
                {
                    Publish(new WordRejectedEvent(word, meta.Cost));
                    return;
                }
            }

            var resolved = ResolveActions(word, actions, meta);

            Publish(new ActionExecutionStartedEvent(word, resolved.Count));

            bool isInstant = _animationResolver == null || _animationResolver.IsInstant;

            if (isInstant)
            {
                ExecuteAllImmediately(resolved);
                Publish(new ActionResolvedEvent(word, resolved, _combatContext.SourceEntity, true));
                Publish(new ActionExecutionCompletedEvent(word));
            }
            else
            {
                Publish(new ActionResolvedEvent(word, resolved, _combatContext.SourceEntity, false));
            }
        }

        private List<ResolvedAction> ResolveActions(string word, IReadOnlyList<WordActionMapping> actions, WordMeta meta)
        {
            var resolved = new List<ResolvedAction>(actions.Count);
            var seenActions = new HashSet<string>();

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];

                // Item actions may have multiple rows (one per ammo type) — only resolve once
                if (string.Equals(mapping.ActionId, "Item", StringComparison.OrdinalIgnoreCase))
                {
                    if (!seenActions.Add(mapping.ActionId)) continue;
                }

                var actionTarget = mapping.Target ?? meta.Target;

                var spec = TargetTypeClassifier.Parse(actionTarget);
                var targets = _combatContext.GetTargets(spec.BaseType, 0, spec.StatusFilter);

                if (targets.Count == 1 && _statusEffects != null &&
                    _statusEffects.HasEffect(targets[0], StatusEffectType.Reflecting))
                {
                    var reflectTarget = targets[0];
                    targets = new[] { _combatContext.SourceEntity };
                    _statusEffects.DecrementStack(reflectTarget, StatusEffectType.Reflecting);
                }

                if (_handlerRegistry.TryGet(mapping.ActionId, out _))
                {
                    resolved.Add(new ResolvedAction(
                        mapping.ActionId, mapping.Value,
                        _combatContext.SourceEntity, targets, word));
                }
            }

            return resolved;
        }

        private void ExecuteAllImmediately(List<ResolvedAction> resolved)
        {
            foreach (var action in resolved)
            {
                if (_handlerRegistry.TryGet(action.ActionId, out var handler))
                {
                    var context = new ActionContext(action.Source, action.Targets, action.Value, action.Word);
                    handler.Execute(context);
                    Publish(new ActionHandlerExecutedEvent(action.ActionId, action.Value, action.Source, action.Targets));
                }
            }
        }
    }
}
