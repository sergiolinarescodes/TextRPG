using System;
using System.Collections.Generic;
using System.Linq;

namespace TextRPG.Core.WordAction
{
    internal sealed class FilteredWordResolver : IWordResolver
    {
        private readonly IWordResolver _inner;
        private readonly HashSet<string> _excludedWords;

        public FilteredWordResolver(IWordResolver inner, HashSet<string> excludedWords)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _excludedWords = excludedWords ?? throw new ArgumentNullException(nameof(excludedWords));
        }

        public IReadOnlyList<WordActionMapping> Resolve(string word)
        {
            if (IsExcluded(word)) return Array.Empty<WordActionMapping>();
            return _inner.Resolve(word);
        }

        public WordMeta GetStats(string word)
        {
            if (IsExcluded(word)) return default;
            return _inner.GetStats(word);
        }

        public bool HasWord(string word)
        {
            if (IsExcluded(word)) return false;
            return _inner.HasWord(word);
        }

        public int WordCount => _inner.WordCount;

        public IEnumerable<string> AllWords => _inner.AllWords.Where(w => !IsExcluded(w));

        private bool IsExcluded(string word)
        {
            return word != null && _excludedWords.Contains(word.ToLowerInvariant());
        }
    }
}
