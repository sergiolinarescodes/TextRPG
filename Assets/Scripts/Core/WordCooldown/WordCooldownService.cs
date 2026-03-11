using System;
using System.Collections.Generic;

namespace TextRPG.Core.WordCooldown
{
    internal sealed class WordCooldownService : IWordCooldownService
    {
        private static readonly int[] Schedule = { 2, 5, 10, 20, -1 };

        private readonly Dictionary<string, (int UseCount, int LastUsedRound)> _usage = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _fixedCooldowns = new(StringComparer.OrdinalIgnoreCase);

        public bool CanUseWord(string word, int currentRound)
        {
            if (!_usage.TryGetValue(word, out var entry))
                return true;
            if (entry.UseCount <= 0)
                return true;

            if (_fixedCooldowns.TryGetValue(word, out var fixedCd))
                return currentRound >= entry.LastUsedRound + fixedCd;

            var idx = Math.Min(entry.UseCount - 1, Schedule.Length - 1);
            var cooldown = Schedule[idx];
            if (cooldown == -1)
                return false;

            return currentRound >= entry.LastUsedRound + cooldown;
        }

        public void MarkWordUsed(string word, int currentRound)
        {
            if (_fixedCooldowns.ContainsKey(word))
            {
                // Fixed-cooldown words always stay at UseCount=1 (no escalation)
                _usage[word] = (1, currentRound);
                return;
            }

            if (_usage.TryGetValue(word, out var entry))
                _usage[word] = (entry.UseCount + 1, currentRound);
            else
                _usage[word] = (1, currentRound);
        }

        public int GetRemainingCooldown(string word, int currentRound)
        {
            if (!_usage.TryGetValue(word, out var entry))
                return 0;
            if (entry.UseCount <= 0)
                return 0;

            if (_fixedCooldowns.TryGetValue(word, out var fixedCd))
            {
                var readyAtFixed = entry.LastUsedRound + fixedCd;
                return Math.Max(0, readyAtFixed - currentRound);
            }

            var idx = Math.Min(entry.UseCount - 1, Schedule.Length - 1);
            var cooldown = Schedule[idx];
            if (cooldown == -1)
                return -1;

            var readyAt = entry.LastUsedRound + cooldown;
            return Math.Max(0, readyAt - currentRound);
        }

        public int GetUseCount(string word)
        {
            return _usage.TryGetValue(word, out var entry) ? entry.UseCount : 0;
        }

        public bool IsPermanentlyBanned(string word)
        {
            if (_fixedCooldowns.ContainsKey(word))
                return false;

            if (!_usage.TryGetValue(word, out var entry))
                return false;
            if (entry.UseCount <= 0)
                return false;

            var idx = Math.Min(entry.UseCount - 1, Schedule.Length - 1);
            return Schedule[idx] == -1;
        }

        public void RegisterFixedCooldown(string word, int rounds)
        {
            _fixedCooldowns[word] = rounds;
        }

        public void Reset()
        {
            _usage.Clear();
            // Fixed cooldowns persist across encounters (spells are run-lifetime)
        }
    }
}
