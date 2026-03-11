using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    public interface IWordTagResolver
    {
        IReadOnlyList<string> GetTags(string word);
        IReadOnlyList<string> GetWordsByTag(string tag);
        string GetRandomWordByTag(string tag);
        bool HasTag(string word, string tag);
        void AddTag(string word, string tag);
        IEnumerable<string> AllTags { get; }
    }
}
