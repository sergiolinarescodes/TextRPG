using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    public interface IWordResolver
    {
        IReadOnlyList<WordActionMapping> Resolve(string word);
        WordMeta GetStats(string word);
        bool HasWord(string word);
        int WordCount { get; }
        IEnumerable<string> AllWords { get; }
    }
}
