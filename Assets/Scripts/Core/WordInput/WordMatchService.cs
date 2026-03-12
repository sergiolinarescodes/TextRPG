using System;
using System.Collections.Generic;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.WordInput
{
    internal sealed class WordMatchService : IWordMatchService
    {
        private static readonly IReadOnlyList<CharActionColor> Empty = Array.Empty<CharActionColor>();
        private static readonly Color MatchedGrey = new(0.55f, 0.55f, 0.55f);

        private readonly IWordResolver _wordResolver;
        private readonly IActionRegistry _actionRegistry;

        public bool IsMatched { get; private set; }

        public WordMatchService(IWordResolver wordResolver, IActionRegistry actionRegistry)
        {
            _wordResolver = wordResolver;
            _actionRegistry = actionRegistry;
        }

        public IReadOnlyList<CharActionColor> CheckMatch(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                IsMatched = false;
                return Empty;
            }

            var word = text.ToLowerInvariant();

            if (!_wordResolver.HasWord(word))
            {
                IsMatched = false;
                return Empty;
            }

            var actions = _wordResolver.Resolve(word);
            if (actions.Count == 0)
            {
                IsMatched = false;
                return Empty;
            }

            // All matched letters get uniform grey; effect-based coloring
            // (attune, drunk, letter challenge) is applied as overlay in the controller
            var charCount = word.Length;
            var result = new CharActionColor[charCount];
            for (int i = 0; i < charCount; i++)
                result[i] = new CharActionColor(i, null, MatchedGrey);

            IsMatched = true;
            return result;
        }
    }
}
