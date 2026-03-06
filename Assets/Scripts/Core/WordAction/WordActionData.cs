namespace TextRPG.Core.WordAction
{
    internal sealed class WordActionData
    {
        public IWordResolver Resolver { get; }
        public IActionRegistry ActionRegistry { get; }

        public WordActionData(IWordResolver resolver, IActionRegistry actionRegistry)
        {
            Resolver = resolver;
            ActionRegistry = actionRegistry;
        }
    }
}
