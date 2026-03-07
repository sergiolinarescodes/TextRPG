using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    internal sealed class WordActionData
    {
        public IWordResolver Resolver { get; }
        public IActionRegistry ActionRegistry { get; }
        public IWordTagResolver TagResolver { get; }
        public HashSet<string> AmmoWordSet { get; }
        public IWordResolver AmmoResolver { get; }

        public WordActionData(
            IWordResolver resolver,
            IActionRegistry actionRegistry,
            IWordTagResolver tagResolver,
            HashSet<string> ammoWordSet,
            IWordResolver ammoResolver)
        {
            Resolver = resolver;
            ActionRegistry = actionRegistry;
            TagResolver = tagResolver;
            AmmoWordSet = ammoWordSet;
            AmmoResolver = ammoResolver;
        }
    }
}
