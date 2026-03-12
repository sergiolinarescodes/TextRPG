using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.LetterReserve
{
    /// <summary>
    /// Applies +20% bonus per consumed reserved letter to action values.
    /// Only applies to the owner (player) — enemy words do not consume reserved letters.
    /// Caches the bonus per-word so multi-action words consume letters only once.
    /// Resets cache on ActionExecutionCompletedEvent.
    /// </summary>
    internal sealed class LetterReserveValueModifier : IActionValueModifier
    {
        private const float BonusPerLetter = 0.2f;

        private readonly ILetterReserveService _reserveService;
        private readonly EntityId _ownerId;

        private string _lastWord;
        private int _lastCount;

        public LetterReserveValueModifier(ILetterReserveService reserveService, IEventBus eventBus, EntityId ownerId)
        {
            _reserveService = reserveService;
            _ownerId = ownerId;
            eventBus.Subscribe<ActionExecutionCompletedEvent>(_ =>
            {
                _lastWord = null;
                _lastCount = 0;
            });
        }

        public int ModifyValue(string actionId, int baseValue, string word, EntityId source)
        {
            // Only the owner benefits from reserved letters — skip enemy words entirely
            if (source != _ownerId) return baseValue;

            // Attune is a utility action — never consume letters for it
            if (actionId == ActionNames.Attune) return baseValue;

            if (word != _lastWord)
            {
                _lastWord = word;
                _lastCount = _reserveService.ConsumeMatching(word);
            }

            if (_lastCount == 0) return baseValue;
            return (int)(baseValue * (1.0f + BonusPerLetter * _lastCount));
        }
    }
}
