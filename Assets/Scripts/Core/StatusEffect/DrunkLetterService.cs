using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.StatusEffect
{
    public readonly record struct DrunkLettersChangedEvent(IReadOnlyDictionary<char, char> ScrambleMap);

    internal sealed class DrunkLetterService : SystemServiceBase, IDrunkLetterService
    {
        private const int DrunkLettersPerStack = 1;
        private const int ConcussionLettersPerStack = 1;

        private readonly EntityId _playerId;
        private readonly Dictionary<char, char> _scrambleMap = new();
        private readonly HashSet<char> _remappedChars = new();
        private readonly char[] _alphabet;
        private int _currentStacks;
        private int _drunkStacks;
        private int _concussionStacks;

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
            if (!e.Target.Equals(_playerId)) return;
            if (e.Type == StatusEffectType.Drunk || e.Type == StatusEffectType.Concussion)
                UpdateStacks();
        }

        private void OnEffectRemoved(StatusEffectRemovedEvent e)
        {
            if (!e.Target.Equals(_playerId)) return;
            if (e.Type == StatusEffectType.Drunk || e.Type == StatusEffectType.Concussion)
                UpdateStacks();
        }

        private void OnEffectTicked(StatusEffectTickedEvent e)
        {
            if (!e.Target.Equals(_playerId)) return;
            if (e.Type == StatusEffectType.Drunk || e.Type == StatusEffectType.Concussion)
                UpdateStacks();
        }

        private void OnEffectExpired(StatusEffectExpiredEvent e)
        {
            if (!e.Target.Equals(_playerId)) return;
            if (e.Type == StatusEffectType.Drunk || e.Type == StatusEffectType.Concussion)
                UpdateStacks();
        }

        private void UpdateStacks()
        {
            if (_statusEffects == null) return;
            _drunkStacks = _statusEffects.GetStackCount(_playerId, StatusEffectType.Drunk);
            _concussionStacks = _statusEffects.GetStackCount(_playerId, StatusEffectType.Concussion);
            int totalLetters = _drunkStacks * DrunkLettersPerStack + _concussionStacks * ConcussionLettersPerStack;
            if (totalLetters == _currentStacks) return;
            _currentStacks = totalLetters;
            Randomize(totalLetters);
        }

        private IStatusEffectService _statusEffects;

        public void SetStatusEffects(IStatusEffectService service)
        {
            _statusEffects = service;
        }

        private void Randomize(int letterCount)
        {
            _scrambleMap.Clear();
            _remappedChars.Clear();

            if (letterCount <= 0)
            {
                Publish(new DrunkLettersChangedEvent(_scrambleMap));
                return;
            }

            int count = System.Math.Min(letterCount, 26);

            // Fisher-Yates shuffle to pick which letters get scrambled
            var shuffled = (char[])_alphabet.Clone();
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // For each picked letter, map to a random different letter from the full alphabet
            for (int i = 0; i < count; i++)
            {
                char source = shuffled[i];
                char target;
                do
                {
                    target = _alphabet[UnityEngine.Random.Range(0, 26)];
                } while (target == source);

                _scrambleMap[source] = target;
                _remappedChars.Add(target);
            }

            Publish(new DrunkLettersChangedEvent(_scrambleMap));
        }

        private void Clear()
        {
            _currentStacks = 0;
            _drunkStacks = 0;
            _concussionStacks = 0;
            _scrambleMap.Clear();
            _remappedChars.Clear();
            Publish(new DrunkLettersChangedEvent(_scrambleMap));
        }
    }
}
