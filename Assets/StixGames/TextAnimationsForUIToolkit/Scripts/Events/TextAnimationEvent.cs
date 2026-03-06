using System;

namespace TextAnimationsForUIToolkit.Events
{
    public abstract class TextAnimationEvent : IComparable<TextAnimationEvent>
    {
        protected internal readonly float eventTime;

        protected TextAnimationEvent(float eventTime)
        {
            this.eventTime = eventTime;
        }

        public int CompareTo(TextAnimationEvent other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            return eventTime.CompareTo(other.eventTime);
        }
    }
}
