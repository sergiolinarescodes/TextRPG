using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;

namespace TextRPG.Core.LetterReserve
{
    internal sealed class LetterReserveService : SystemServiceBase, ILetterReserveService
    {
        private readonly List<char> _letters = new();

        public LetterReserveService(IEventBus eventBus) : base(eventBus) { }

        public void AddLetters(string word, string source)
        {
            if (string.IsNullOrEmpty(word)) return;

            foreach (var c in word)
            {
                if (char.IsLetter(c))
                    _letters.Add(char.ToLowerInvariant(c));
            }

            Publish(new LetterReserveChangedEvent(_letters.AsReadOnly()));
        }

        public int ConsumeMatching(string word)
        {
            if (string.IsNullOrEmpty(word) || _letters.Count == 0) return 0;

            int consumed = 0;
            // Work on a temporary copy of indices to remove
            var toRemove = new List<int>();

            foreach (var c in word)
            {
                var lower = char.ToLowerInvariant(c);
                for (int i = 0; i < _letters.Count; i++)
                {
                    if (toRemove.Contains(i)) continue;
                    if (_letters[i] == lower)
                    {
                        toRemove.Add(i);
                        consumed++;
                        break;
                    }
                }
            }

            if (consumed > 0)
            {
                // Remove in reverse order to preserve indices
                toRemove.Sort();
                for (int i = toRemove.Count - 1; i >= 0; i--)
                    _letters.RemoveAt(toRemove[i]);

                Publish(new LetterReserveChangedEvent(_letters.AsReadOnly()));
            }

            return consumed;
        }

        public bool IsLetterReserved(char c)
        {
            var lower = char.ToLowerInvariant(c);
            return _letters.Contains(lower);
        }

        public IReadOnlyList<char> GetReservedLetters() => _letters.AsReadOnly();

        public void Clear()
        {
            if (_letters.Count == 0) return;
            _letters.Clear();
            Publish(new LetterReserveChangedEvent(_letters.AsReadOnly()));
        }
    }
}
