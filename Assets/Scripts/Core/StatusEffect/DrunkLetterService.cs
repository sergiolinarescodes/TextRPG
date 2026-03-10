using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.StatusEffect
{
    public readonly record struct DrunkLettersChangedEvent(IReadOnlyDictionary<char, char> ScrambleMap);

    internal sealed class DrunkLetterService : SystemServiceBase, IDrunkLetterService
    {
        private const int LettersPerStack = 5;

        private readonly EntityId _playerId;
        private readonly Dictionary<char, char> _scrambleMap = new();
        private readonly HashSet<char> _remappedChars = new();
        private readonly char[] _alphabet;
        private int _currentStacks;

        public bool IsActive => _currentStacks > 0;
        public int CurrentStacks => _currentStacks;

        public DrunkLetterService(IEventBus eventBus, EntityId playerId) : base(eventBus)
        {
            _playerId = playerId;
            _alphabet = new char[26];
            for (int i = 0; i < 26; i++)
                _alphabet[i] = (char)('a' + i);

            Subscribe<StatusEffectAppliedEvent>(OnEffectApplied);
            Subscribe<StatusEffectRemovedEvent>(OnEffectRemoved);
            Subscribe<StatusEffectTickedEvent>(OnEffectTicked);
            Subscribe<StatusEffectExpiredEvent>(OnEffectExpired);
        }

        public char RemapInput(char input)
        {
            var lower = char.ToLowerInvariant(input);
            return _scrambleMap.TryGetValue(lower, out var replacement) ? replacement : lower;
        }

        public bool IsLetterDrunk(char letter)
        {
            return _scrambleMap.ContainsKey(char.ToLowerInvariant(letter));
        }

        public bool IsRemappedChar(char c)
        {
            return _remappedChars.Contains(char.ToLowerInvariant(c));
        }

        public IReadOnlyDictionary<char, char> GetScrambleMap() => _scrambleMap;

        private void OnEffectApplied(StatusEffectAppliedEvent e)
        {
            if (e.Type != StatusEffectType.Drunk || !e.Target.Equals(_playerId)) return;
            UpdateStacks(e.Target);
        }

        private void OnEffectRemoved(StatusEffectRemovedEvent e)
        {
            if (e.Type != StatusEffectType.Drunk || !e.Target.Equals(_playerId)) return;
            Clear();
        }

        private void OnEffectTicked(StatusEffectTickedEvent e)
        {
            if (e.Type != StatusEffectType.Drunk || !e.Target.Equals(_playerId)) return;
            UpdateStacks(e.Target);
        }

        private void OnEffectExpired(StatusEffectExpiredEvent e)
        {
            if (e.Type != StatusEffectType.Drunk || !e.Target.Equals(_playerId)) return;
            Clear();
        }

        private void UpdateStacks(EntityId target)
        {
            // We need the IStatusEffectService to get stack count, but we subscribe to events
            // which carry the change. For StackIntensity effects, AppliedEvent fires after stacks change.
            // We'll read the stack count from StatusEffectService if available via a callback,
            // but the simplest approach: use the Duration field from the event as proxy.
            // Actually, events don't carry stack count directly. We need the service reference.
            // The plan says to react to events and call Randomize with new stack count.
            // We'll accept IStatusEffectService as optional param.
            // For now, rely on being able to call _statusEffects.GetStackCount().

            // This is handled by injecting IStatusEffectService via a setter (same pattern as StatusEffectHandlerContext).
            if (_statusEffects == null) return;
            var stacks = _statusEffects.GetStackCount(target, StatusEffectType.Drunk);
            if (stacks == _currentStacks) return;
            _currentStacks = stacks;
            Randomize(stacks);
        }

        private IStatusEffectService _statusEffects;

        public void SetStatusEffects(IStatusEffectService service)
        {
            _statusEffects = service;
        }

        private void Randomize(int stacks)
        {
            _scrambleMap.Clear();
            _remappedChars.Clear();

            if (stacks <= 0)
            {
                Publish(new DrunkLettersChangedEvent(_scrambleMap));
                return;
            }

            int count = System.Math.Min(stacks * LettersPerStack, 26);

            // Fisher-Yates shuffle on a copy of the alphabet
            var shuffled = (char[])_alphabet.Clone();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // Pick first 'count' letters to scramble
            var picked = new char[count];
            System.Array.Copy(shuffled, picked, count);

            // For each picked letter, assign a random different replacement
            // Shuffle the picked array again and use as replacement targets
            var replacements = (char[])picked.Clone();
            // Ensure no letter maps to itself (derangement)
            for (int attempts = 0; attempts < 100; attempts++)
            {
                for (int i = replacements.Length - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (replacements[i], replacements[j]) = (replacements[j], replacements[i]);
                }

                bool hasFixedPoint = false;
                for (int i = 0; i < picked.Length; i++)
                {
                    if (picked[i] == replacements[i])
                    {
                        hasFixedPoint = true;
                        break;
                    }
                }

                if (!hasFixedPoint) break;
            }

            for (int i = 0; i < picked.Length; i++)
            {
                _scrambleMap[picked[i]] = replacements[i];
                _remappedChars.Add(replacements[i]);
            }

            Publish(new DrunkLettersChangedEvent(_scrambleMap));
        }

        private void Clear()
        {
            _currentStacks = 0;
            _scrambleMap.Clear();
            _remappedChars.Clear();
            Publish(new DrunkLettersChangedEvent(_scrambleMap));
        }
    }
}
