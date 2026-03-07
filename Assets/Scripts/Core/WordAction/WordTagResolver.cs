using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TextRPG.Core.WordAction
{
    internal sealed class WordTagResolver : IWordTagResolver
    {
        private static readonly IReadOnlyList<string> EmptyList = new List<string>();

        private readonly Dictionary<string, List<string>> _wordToTags;
        private readonly Dictionary<string, HashSet<string>> _tagToWords;

        public WordTagResolver(Dictionary<string, List<string>> wordToTags)
        {
            _wordToTags = new Dictionary<string, List<string>>();
            _tagToWords = new Dictionary<string, HashSet<string>>();

            foreach (var (word, tags) in wordToTags)
            {
                var normalizedWord = word.ToLowerInvariant();
                var normalizedTags = new List<string>();

                foreach (var tag in tags)
                {
                    var normalizedTag = TagNormalizer.Normalize(tag);
                    if (string.IsNullOrEmpty(normalizedTag)) continue;

                    normalizedTags.Add(normalizedTag);

                    if (!_tagToWords.TryGetValue(normalizedTag, out var wordSet))
                    {
                        wordSet = new HashSet<string>();
                        _tagToWords[normalizedTag] = wordSet;
                    }

                    wordSet.Add(normalizedWord);
                }

                _wordToTags[normalizedWord] = normalizedTags;
            }
        }

        public IReadOnlyList<string> GetTags(string word)
        {
            var key = word.ToLowerInvariant();
            return _wordToTags.TryGetValue(key, out var tags) ? tags : EmptyList;
        }

        public IReadOnlyList<string> GetWordsByTag(string tag)
        {
            var key = TagNormalizer.Normalize(tag);
            return _tagToWords.TryGetValue(key, out var words) ? words.ToList() : EmptyList;
        }

        public string GetRandomWordByTag(string tag)
        {
            var key = TagNormalizer.Normalize(tag);
            if (!_tagToWords.TryGetValue(key, out var words) || words.Count == 0) return null;
            int index = Random.Range(0, words.Count);
            foreach (var w in words)
            {
                if (index == 0) return w;
                index--;
            }
            return null;
        }

        public bool HasTag(string word, string tag)
        {
            var tags = GetTags(word);
            var normalizedTag = TagNormalizer.Normalize(tag);
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == normalizedTag) return true;
            }
            return false;
        }

        public IEnumerable<string> AllTags => _tagToWords.Keys;
    }
}
