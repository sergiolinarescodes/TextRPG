using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;

namespace TextRPG.Core.LetterChallenge
{
    internal sealed class LetterChallengeService : ILetterChallengeService
    {
        private static readonly char[] Vowels = { 'a', 'e', 'i', 'o', 'u' };
        private static readonly char[] Consonants =
        {
            'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm',
            'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'y', 'z'
        };
        private static readonly char[] AllLetters =
        {
            'a','b','c','d','e','f','g','h','i','j','k','l','m',
            'n','o','p','q','r','s','t','u','v','w','x','y','z'
        };

        private readonly IEventBus _eventBus;
        private readonly Random _rng = new();

        private readonly Dictionary<EntityId, ChallengeState> _challenges = new();

        public LetterChallengeService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string SelectLetters(EntityId owner, string mode)
        {
            ParseMode(mode, out var selectionMode, out var selectionParam,
                out var matchType, out var matchParam);

            var letters = SelectByMode(selectionMode, selectionParam);

            _challenges[owner] = new ChallengeState(letters, matchType, matchParam, mode);
            _eventBus.Publish(new LetterChallengeStartedEvent(owner, letters, mode));

            return letters;
        }

        public string GetActiveLetters(EntityId owner)
        {
            return _challenges.TryGetValue(owner, out var state) ? state.Letters : null;
        }

        public bool CheckWord(EntityId owner, string word)
        {
            if (!_challenges.TryGetValue(owner, out var state)) return false;
            if (string.IsNullOrEmpty(word)) return false;

            var lowerWord = word.ToLowerInvariant();
            bool matched = state.MatchType switch
            {
                "starts_with" => MatchStartsWith(lowerWord, state.Letters),
                "ends_with" => MatchEndsWith(lowerWord, state.Letters),
                "position" => MatchPosition(lowerWord, state.Letters, state.MatchParam),
                "all" => MatchAll(lowerWord, state.Letters),
                _ => MatchContains(lowerWord, state.Letters), // "contains" default
            };

            if (matched)
                _eventBus.Publish(new LetterChallengeMatchedEvent(owner, state.Letters, word));

            return matched;
        }

        public void Clear(EntityId owner)
        {
            if (_challenges.Remove(owner))
                _eventBus.Publish(new LetterChallengeClearedEvent(owner));
        }

        // --- Mode parsing ---

        private static void ParseMode(string mode, out string selectionMode, out string selectionParam,
            out string matchType, out string matchParam)
        {
            selectionMode = "vowel";
            selectionParam = null;
            matchType = "contains";
            matchParam = null;

            if (string.IsNullOrEmpty(mode)) return;

            // Split on ':' — first part(s) are selection, rest are match
            // Examples: "vowel", "fixed:e", "vowel:starts_with", "fixed:e:position:3", "multi:aei:all"
            var parts = mode.Split(':');

            int matchStart = ParseSelection(parts, out selectionMode, out selectionParam);

            if (matchStart < parts.Length)
            {
                matchType = parts[matchStart];
                if (matchStart + 1 < parts.Length)
                    matchParam = parts[matchStart + 1];
            }
        }

        private static int ParseSelection(string[] parts, out string selectionMode, out string selectionParam)
        {
            selectionMode = parts[0];
            selectionParam = null;

            switch (selectionMode)
            {
                case "fixed" when parts.Length > 1:
                    selectionParam = parts[1];
                    return 2; // match starts at index 2
                case "multi" when parts.Length > 1:
                    selectionParam = parts[1];
                    return 2;
                default:
                    return 1; // match starts at index 1
            }
        }

        // --- Letter selection ---

        private string SelectByMode(string selectionMode, string selectionParam)
        {
            return selectionMode switch
            {
                "vowel" => RandomFrom(Vowels).ToString(),
                "consonant" => RandomFrom(Consonants).ToString(),
                "any" => RandomFrom(AllLetters).ToString(),
                "fixed" => selectionParam?.ToLowerInvariant() ?? "a",
                "multi" => selectionParam?.ToLowerInvariant() ?? "aei",
                _ => RandomFrom(Vowels).ToString(),
            };
        }

        private char RandomFrom(char[] pool) => pool[_rng.Next(pool.Length)];

        // --- Matching ---

        private static bool MatchContains(string word, string letters)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                if (word.Contains(letters[i])) return true;
            }
            return false;
        }

        private static bool MatchStartsWith(string word, string letters)
        {
            if (word.Length == 0) return false;
            return letters.Contains(word[0]);
        }

        private static bool MatchEndsWith(string word, string letters)
        {
            if (word.Length == 0) return false;
            return letters.Contains(word[^1]);
        }

        private static bool MatchPosition(string word, string letters, string matchParam)
        {
            if (!int.TryParse(matchParam, out var pos)) return false;
            if (pos < 0 || pos >= word.Length) return false;
            return letters.Contains(word[pos]);
        }

        private static bool MatchAll(string word, string letters)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                if (!word.Contains(letters[i])) return false;
            }
            return true;
        }

        private readonly record struct ChallengeState(string Letters, string MatchType, string MatchParam, string RawMode);
    }
}
