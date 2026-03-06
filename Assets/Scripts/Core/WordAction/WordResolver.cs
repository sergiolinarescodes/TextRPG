using System;
using System.Collections.Generic;

namespace TextRPG.Core.WordAction
{
    internal sealed class WordResolver : IWordResolver
    {
        private readonly Dictionary<string, List<WordActionMapping>> _mappings;
        private readonly Dictionary<string, WordMeta> _stats;

        public WordResolver(
            Dictionary<string, List<WordActionMapping>> mappings,
            Dictionary<string, WordMeta> stats)
        {
            _mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public IReadOnlyList<WordActionMapping> Resolve(string word)
        {
            if (word == null) return Array.Empty<WordActionMapping>();
            return _mappings.TryGetValue(word.ToLowerInvariant(), out var actions)
                ? actions
                : Array.Empty<WordActionMapping>();
        }

        public WordMeta GetStats(string word)
        {
            if (word == null) return default;
            return _stats.TryGetValue(word.ToLowerInvariant(), out var stats)
                ? stats
                : default;
        }

        public bool HasWord(string word)
        {
            return word != null && _mappings.ContainsKey(word.ToLowerInvariant());
        }

        public int WordCount => _mappings.Count;
    }
}
