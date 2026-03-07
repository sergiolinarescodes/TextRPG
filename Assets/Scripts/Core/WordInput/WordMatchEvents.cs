using System.Collections.Generic;

namespace TextRPG.Core.WordInput
{
    public readonly record struct WordMatchedEvent(string Word, IReadOnlyList<CharActionColor> CharColors);
    public readonly record struct WordMatchClearedEvent();
}
