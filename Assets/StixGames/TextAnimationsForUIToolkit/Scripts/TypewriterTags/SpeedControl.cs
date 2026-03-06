using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;

namespace TextAnimationsForUIToolkit.TypewriterTags
{
    /// <summary>
    /// Overwrite the current animation speed.
    ///
    /// Closing the tag cancels all animation speed changes.
    /// </summary>
    internal class OpenAnimationSpeedModifier : TypewriterTag
    {
        /// <summary>
        /// Speed as fraction
        /// </summary>
        public float speed { get; }

        public OpenAnimationSpeedModifier(float speed)
        {
            this.speed = speed;
        }
    }

    internal class CloseAnimationSpeedModifier : TypewriterTag { }

    internal class SpeedControlTagParser : ControlTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "speed";
        }

        public override TextUnit OpenTag(string tagName, Parameters parameters)
        {
            if (!parameters.TryGetMainFloatValue(out var speed, canBePercent: true))
            {
                throw new ArgumentException("Main parameter must be a float");
            }

            return new OpenAnimationSpeedModifier(speed);
        }

        public override TextUnit CloseTag(string tagName)
        {
            return new CloseAnimationSpeedModifier();
        }
    }
}
