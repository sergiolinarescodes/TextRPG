using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.WordAction;

namespace TextRPG.Core.Encounter
{
    internal sealed class CompositeWordResolver : IWordResolver
    {
        private readonly IWordResolver[] _resolvers;

        public CompositeWordResolver(params IWordResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public IReadOnlyList<WordActionMapping> Resolve(string word)
        {
            foreach (var resolver in _resolvers)
            {
                if (resolver.HasWord(word))
                    return resolver.Resolve(word);
            }
            return Array.Empty<WordActionMapping>();
        }

        public WordMeta GetStats(string word)
        {
            foreach (var resolver in _resolvers)
            {
                if (resolver.HasWord(word))
                    return resolver.GetStats(word);
            }
            return default;
        }

        public bool HasWord(string word)
        {
            foreach (var resolver in _resolvers)
            {
                if (resolver.HasWord(word))
                    return true;
            }
            return false;
        }

        public int WordCount
        {
            get
            {
                int total = 0;
                foreach (var resolver in _resolvers)
                    total += resolver.WordCount;
                return total;
            }
        }

        public IEnumerable<string> AllWords => _resolvers.SelectMany(r => r.AllWords);
    }
}
