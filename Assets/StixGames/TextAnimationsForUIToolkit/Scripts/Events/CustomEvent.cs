namespace TextAnimationsForUIToolkit.Events
{
    public class CustomEvent : TextAnimationEvent
    {
        public readonly string name;

        public CustomEvent(float eventTime, string name)
            : base(eventTime)
        {
            this.name = name;
        }
    }
}