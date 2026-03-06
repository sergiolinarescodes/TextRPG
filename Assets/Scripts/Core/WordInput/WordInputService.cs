using System.Text;
using Unidad.Core.EventBus;

namespace TextRPG.Core.WordInput
{
    internal sealed class WordInputService : IWordInputService
    {
        private readonly IEventBus _eventBus;
        private readonly StringBuilder _buffer = new();

        public string CurrentWord => _buffer.ToString();

        public WordInputService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void AppendCharacter(char c)
        {
            _buffer.Append(c);
            _eventBus.Publish(new WordCharacterAddedEvent(c, CurrentWord));
        }

        public void RemoveLastCharacter()
        {
            if (_buffer.Length == 0) return;
            _buffer.Remove(_buffer.Length - 1, 1);
            _eventBus.Publish(new WordCharacterAddedEvent('\0', CurrentWord));
        }

        public void SubmitWord()
        {
            var word = CurrentWord;
            if (word.Length == 0) return;
            _buffer.Clear();
            _eventBus.Publish(new WordSubmittedEvent(word));
        }

        public void Clear()
        {
            _buffer.Clear();
            _eventBus.Publish(new WordClearedEvent());
        }
    }
}
