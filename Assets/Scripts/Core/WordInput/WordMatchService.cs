using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.WordInput
{
    internal sealed class WordMatchService : IWordMatchService
    {
        private static readonly IReadOnlyList<CharActionColor> Empty = Array.Empty<CharActionColor>();

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

            var charCount = word.Length;
            var totalValue = 0;
            foreach (var a in actions)
                totalValue += a.Value;

            if (totalValue <= 0)
            {
                IsMatched = true;
                return Empty;
            }

            // Largest Remainder Method
            var shares = new (string ActionId, Color Color, int Floor, double Remainder)[actions.Count];
            var floorSum = 0;

            for (int i = 0; i < actions.Count; i++)
            {
                var exactShare = (double)actions[i].Value / totalValue * charCount;
                var floor = (int)Math.Floor(exactShare);
                var remainder = exactShare - floor;

                if (!_actionRegistry.TryGet(actions[i].ActionId, out var def))
                    def = new ActionDefinition(actions[i].ActionId, actions[i].ActionId, Color.white);

                shares[i] = (actions[i].ActionId, def.Color, floor, remainder);
                floorSum += floor;
            }

            // Distribute leftover chars to highest remainders
            var leftover = charCount - floorSum;
            var indices = Enumerable.Range(0, shares.Length).OrderByDescending(i => shares[i].Remainder).ToArray();
            for (int j = 0; j < leftover && j < indices.Length; j++)
            {
                var idx = indices[j];
                shares[idx] = (shares[idx].ActionId, shares[idx].Color, shares[idx].Floor + 1, shares[idx].Remainder);
            }

            // Assign characters sequentially
            var result = new CharActionColor[charCount];
            var charIdx = 0;
            for (int i = 0; i < shares.Length; i++)
            {
                for (int c = 0; c < shares[i].Floor && charIdx < charCount; c++, charIdx++)
                {
                    result[charIdx] = new CharActionColor(charIdx, shares[i].ActionId, shares[i].Color);
                }
            }

            IsMatched = true;
            return result;
        }
    }
}
