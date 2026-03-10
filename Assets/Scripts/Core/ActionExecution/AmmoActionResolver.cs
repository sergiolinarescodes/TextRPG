using System.Collections.Generic;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution
{
    internal static class AmmoActionResolver
    {
        public static List<ResolvedAction> ResolveActions(
            EntityStats.EntityId source, string word,
            IReadOnlyList<WordActionMapping> actions, WordMeta meta,
            IActionHandlerRegistry handlerRegistry, ICombatContext combatContext)
        {
            var resolved = new List<ResolvedAction>(actions.Count);

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];
                var actionTarget = mapping.Target ?? meta.Target;
                var actionRange = mapping.Range ?? meta.Range;

                var spec = TargetTypeClassifier.Parse(actionTarget);
                var targets = combatContext.GetTargets(spec.BaseType, actionRange, spec.StatusFilter);

                if (handlerRegistry.TryGet(mapping.ActionId, out _))
                {
                    resolved.Add(new ResolvedAction(
                        mapping.ActionId, mapping.Value,
                        source, targets, word));
                }
            }

            return resolved;
        }

        public static void ExecuteAllImmediately(
            List<ResolvedAction> resolved,
            IActionHandlerRegistry handlerRegistry,
            IEventBus eventBus)
        {
            foreach (var action in resolved)
            {
                if (handlerRegistry.TryGet(action.ActionId, out var handler))
                {
                    var context = new ActionContext(action.Source, action.Targets, action.Value, action.Word, action.AssocWord);
                    handler.Execute(context);
                    eventBus.Publish(new ActionHandlerExecutedEvent(action.ActionId, action.Value, action.Source, action.Targets));
                }
            }
        }
    }
}
