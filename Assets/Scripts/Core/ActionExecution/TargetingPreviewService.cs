using System;
using System.Collections.Generic;
using TextRPG.Core.WordAction;

namespace TextRPG.Core.ActionExecution
{
    internal sealed class TargetingPreviewService : ITargetingPreviewService
    {
        private readonly IWordResolver _wordResolver;
        private readonly ICombatContext _combatContext;

        public TargetingPreviewService(IWordResolver wordResolver, ICombatContext combatContext)
        {
            _wordResolver = wordResolver;
            _combatContext = combatContext;
        }

        public TargetingPreview PreviewWord(string word)
        {
            if (!_wordResolver.HasWord(word))
                return new TargetingPreview(Array.Empty<ActionTargetPreview>());

            var actions = _wordResolver.Resolve(word);
            var meta = _wordResolver.GetStats(word);
            var previews = new List<ActionTargetPreview>(actions.Count);

            for (int i = 0; i < actions.Count; i++)
            {
                var mapping = actions[i];
                var actionTarget = mapping.Target ?? meta.Target;

                var spec = TargetTypeClassifier.Parse(actionTarget);
                var targets = _combatContext.GetTargets(spec.BaseType, 0, spec.StatusFilter);

                previews.Add(new ActionTargetPreview(mapping.ActionId, targets));
            }

            return new TargetingPreview(previews);
        }
    }
}
