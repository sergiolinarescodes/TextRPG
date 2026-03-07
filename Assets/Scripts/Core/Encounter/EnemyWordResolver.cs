using System;
using System.Collections.Generic;
using TextRPG.Core.WordAction;

namespace TextRPG.Core.Encounter
{
    internal sealed class EnemyWordResolver : IWordResolver
    {
        private readonly Dictionary<string, List<WordActionMapping>> _mappings = new();
        private readonly Dictionary<string, WordMeta> _stats = new();

        public void RegisterWord(string word, List<WordActionMapping> actions, WordMeta meta)
        {
            var key = word.ToLowerInvariant();
            _mappings[key] = actions;
            _stats[key] = meta;
        }

        public void Clear()
        {
            _mappings.Clear();
            _stats.Clear();
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
            return _stats.TryGetValue(word.ToLowerInvariant(), out var stats) ? stats : default;
        }

        public bool HasWord(string word)
        {
            return word != null && _mappings.ContainsKey(word.ToLowerInvariant());
        }

        public int WordCount => _mappings.Count;

        public IEnumerable<string> AllWords => _mappings.Keys;
    }
}
