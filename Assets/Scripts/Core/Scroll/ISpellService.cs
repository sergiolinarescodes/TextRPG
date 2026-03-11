using System.Collections.Generic;

namespace TextRPG.Core.Scroll
{
    public interface ISpellService
    {
        void LearnSpell(ScrollDefinition scroll);
        bool IsSpell(string word);
        IReadOnlyCollection<string> LearnedSpells { get; }
        IReadOnlyCollection<string> OfferedOriginals { get; }
    }
}
