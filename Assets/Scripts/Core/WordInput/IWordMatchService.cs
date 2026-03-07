using System.Collections.Generic;

namespace TextRPG.Core.WordInput
{
    public interface IWordMatchService
    {
        IReadOnlyList<CharActionColor> CheckMatch(string text);
        bool IsMatched { get; }
    }
}
