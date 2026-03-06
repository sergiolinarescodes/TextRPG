using System;
using UnityEngine.Serialization;

namespace TextAnimationsForUIToolkit.Fades
{
    [Serializable]
    public abstract class FadeAnimation : TextAnimation
    {
        [FormerlySerializedAs("Duration")]
        public float duration = 0.2f;

        protected FadeAnimation()
            : base("#external") { }

        protected FadeAnimation(string creatorTag)
            : base(creatorTag) { }

        /// <summary>
        /// Returns true if the animation is a fade in.
        /// Used to determine if a fallback fade in should be used.
        /// </summary>
        public abstract bool isFadeIn { get; }

        /// <summary>
        /// Returns true if the animation is a fade out.
        /// Used to determine if a fallback fade out should be used.
        /// </summary>
        public abstract bool isFadeOut { get; }
    }
}
