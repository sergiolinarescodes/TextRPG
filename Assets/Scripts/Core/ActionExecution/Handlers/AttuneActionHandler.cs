using TextRPG.Core.LetterReserve;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class AttuneActionHandler : IActionHandler
    {
        private readonly ILetterReserveService _letterReserve;
        private string _lastProcessedWord;

        public string ActionId => ActionNames.Attune;

        public AttuneActionHandler(ILetterReserveService letterReserve, IEventBus eventBus)
        {
            _letterReserve = letterReserve;
            eventBus.Subscribe<ActionExecutionCompletedEvent>(_ => _lastProcessedWord = null);
        }

        public void Execute(ActionContext context)
        {
            // Idempotent: called once during resolution (immediate) and again during animation.
            // Only add letters on the first call per word.
            if (context.Word == _lastProcessedWord) return;
            _lastProcessedWord = context.Word;
            _letterReserve.AddLetters(context.Word, "attune");
        }
    }
}
