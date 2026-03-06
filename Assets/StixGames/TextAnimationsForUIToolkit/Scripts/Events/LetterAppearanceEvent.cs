namespace TextAnimationsForUIToolkit.Events
{
    public class LetterAppearanceEvent : TextAnimationEvent
    {
        public readonly char letter;

        public LetterAppearanceEvent(float eventTime, char letter)
            : base(eventTime)
        {
            this.letter = letter;
        }
    }
}
