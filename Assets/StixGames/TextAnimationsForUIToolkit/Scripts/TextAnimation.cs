using System;
using TextAnimationsForUIToolkit.Data;

namespace TextAnimationsForUIToolkit
{
    [Serializable]
    public abstract class TextAnimation
    {
        public string creatorTag { get; }
        public abstract void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result);

        protected TextAnimation(string creatorTag)
        {
            this.creatorTag = creatorTag;
        }

        /// <summary>
        /// Returns true if the animation animates the color of letters it gets applied to.
        ///
        /// Used for passing optimization hints to UI Toolkit.
        /// </summary>
        public abstract bool animatesColor { get; }

        /// <summary>
        /// Returns true if the animation animates the transforms of letters it gets applied to.
        ///
        /// Used for passing optimization hints to UI Toolkit.
        /// </summary>
        public abstract bool animatesTransform { get; }
    }
}
