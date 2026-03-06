using System.Text;

namespace TextAnimationsForUIToolkit
{
    internal class TextPreprocessor
    {
        public TextAnimationSettings settings { get; set; }

        private readonly StringBuilder _stringBuilder = new();

        public TextPreprocessor(TextAnimationSettings settings)
        {
            this.settings = settings;
        }

        public string Process(string value)
        {
            if (settings == null)
            {
                return value;
            }

            _stringBuilder.Clear();
            _stringBuilder.Append(value);

            foreach (var style in settings.templates)
            {
                _stringBuilder.Replace($"<{style.tag}>", style.openingTag);
                _stringBuilder.Replace($"</{style.tag}>", style.closingTag);
            }

            return _stringBuilder.ToString();
        }
    }
}
