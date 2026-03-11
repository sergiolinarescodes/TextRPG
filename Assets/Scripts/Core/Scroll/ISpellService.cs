using System.Collections.Generic;

namespace TextRPG.Core.Scroll
{
    public interface ISpellService
    {
        void LearnSpell(ScrollDefinition scroll);
        bool IsSpell(string word);
        IReadOnlyList<string> LearnedSpells { get; }
    }
}
