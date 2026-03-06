using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace TextAnimationsForUIToolkit.CustomAnimations
{
    public class CustomAnimationException : Exception
    {
        public CustomAnimationException() { }

        protected CustomAnimationException(
            [NotNull] SerializationInfo info,
            StreamingContext context
        )
            : base(info, context) { }

        public CustomAnimationException(string message)
            : base(message) { }

        public CustomAnimationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}