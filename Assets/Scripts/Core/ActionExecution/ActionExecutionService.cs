using System;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class ActionExecutionService : SystemServiceBase, IActionExecutionService
    {
        private readonly IWordResolver _wordResolver;
        private readonly IActionHandlerRegistry _handlerRegistry;
        private readonly ICombatContext _combatContext;

        public ActionExecutionService(IEventBus eventBus, IWordResolver wordResolver,
            IActionHandlerRegistry handlerRegistry, ICombatContext combatContext)
            : base(eventBus)
        {
            _wordResolver = wordResolver;
            _handlerRegistry = handlerRegistry;
            _combatContext = combatContext;
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

            Publish(new ActionExecutionStartedEvent(word, actions.Count));

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];
                var actionTarget = mapping.Target ?? meta.Target;
                var actionRange = mapping.Range ?? meta.Range;
                var actionArea = mapping.Area ?? meta.Area;

                var targetType = TargetTypeClassifier.ParseTargetType(actionTarget);
                var primaryTargets = _combatContext.GetTargets(targetType, actionRange);
                var targets = _combatContext.ExpandArea(primaryTargets, actionArea);

                if (_handlerRegistry.TryGet(mapping.ActionId, out var handler))
                {
                    var context = new ActionContext(_combatContext.SourceEntity, targets, mapping.Value, word);
                    handler.Execute(context);
                    Publish(new ActionHandlerExecutedEvent(mapping.ActionId, mapping.Value, _combatContext.SourceEntity, targets));
                }
            }

            Publish(new ActionExecutionCompletedEvent(word));
        }
    }
}
