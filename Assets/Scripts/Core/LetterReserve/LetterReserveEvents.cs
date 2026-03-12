using System.Collections.Generic;

namespace TextRPG.Core.LetterReserve
{
    public readonly record struct LetterReserveChangedEvent(IReadOnlyList<char> ReservedLetters);
}
