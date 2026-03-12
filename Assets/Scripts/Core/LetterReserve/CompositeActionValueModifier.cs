using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.LetterReserve
{
    internal sealed class CompositeActionValueModifier : IActionValueModifier
    {
        private readonly IActionValueModifier[] _modifiers;

        public CompositeActionValueModifier(params IActionValueModifier[] modifiers)
        {
            _modifiers = modifiers;
        }

        public int ModifyValue(string actionId, int baseValue, string word, EntityId source)
        {
            var value = baseValue;
            foreach (var modifier in _modifiers)
                value = modifier.ModifyValue(actionId, value, word, source);
            return value;
        }
    }
}
