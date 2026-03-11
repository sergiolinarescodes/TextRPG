using System;
using System.Collections.Generic;
using TextRPG.Core.WordAction;

namespace TextRPG.Core.Encounter
{
    internal sealed class EnemyWordResolver : IWordResolver
    {
        private readonly Dictionary<string, List<WordActionMapping>> _mappings = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WordMeta> _stats = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterWord(string word, List<WordActionMapping> actions, WordMeta meta)
        {
            _mappings[word] = actions;
            _stats[word] = meta;
        }

        public void Clear()
        {
            _mappings.Clear();
            _stats.Clear();
        }

        public IReadOnlyList<WordActionMapping> Resolve(string word)
        {
            if (word == null) return Array.Empty<WordActionMapping>();
            return _mappings.TryGetValue(word, out var actions)
                ? actions
                : Array.Empty<WordActionMapping>();
        }

        public WordMeta GetStats(string word)
        {
            if (word == null) return default;
            return _stats.TryGetValue(word, out var stats) ? stats : default;
        }

        public bool HasWord(string word)
        {
            return word != null && _mappings.ContainsKey(word);
        }

        public int WordCount => _mappings.Count;

        public IEnumerable<string> AllWords => _mappings.Keys;
    }
}
