using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.Scroll
{
    internal static class ScrollGenerator
    {
        public static ScrollDefinition Generate(IWordResolver resolver, HashSet<string> excludeOriginals, System.Random rng)
        {
            var candidates = resolver.AllWords
                .Where(w => !excludeOriginals.Contains(w))
                .Where(w =>
                {
                    var actions = resolver.Resolve(w);
                    return actions.Any(a => a.ActionId == "MagicDamage");
                })
                .ToList();

            if (candidates.Count == 0) return null;

            const int maxAttempts = 20;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var original = candidates[rng.Next(candidates.Count)];
                var scrambled = Scramble(original, rng);

                // Ensure scrambled word doesn't collide with existing words
                if (resolver.HasWord(scrambled))
                    continue;

                var meta = resolver.GetStats(original);
                var manaCost = Math.Max(0, meta.Cost - 1);

                return new ScrollDefinition(scrambled, original, manaCost, ScrollDefinition.ScrollPurple);
            }

            // Fallback: use first candidate even if scramble collides
            var fallback = candidates[rng.Next(candidates.Count)];
            var fallbackScrambled = Scramble(fallback, rng);
            var fallbackMeta = resolver.GetStats(fallback);
            return new ScrollDefinition(fallbackScrambled, fallback,
                Math.Max(0, fallbackMeta.Cost - 1), ScrollDefinition.ScrollPurple);
        }

        private static string Scramble(string word, System.Random rng)
        {
            var chars = word.ToLowerInvariant().ToCharArray();
            if (chars.Length < 3) return new string(chars);

            // Fisher-Yates shuffle, retry if result matches original
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var shuffled = (char[])chars.Clone();
                for (int i = shuffled.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                var result = new string(shuffled);
                if (result != word.ToLowerInvariant())
                    return result;
            }

            // Last resort: swap first two characters
            (chars[0], chars[1]) = (chars[1], chars[0]);
            return new string(chars);
        }
    }
}
