using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Rainbow : TextAnimation
    {
        public Rainbow() : this("#external")
        {
        }

        public Rainbow(string creatorTag)
            : base(creatorTag)
        {
        }

        public float delay = float.NegativeInfinity;

        /// <summary>
        /// The frequency of full circles
        /// </summary>
        public float frequency = 1f;

        public float waveSize = 20f;

        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            if (unit is SpriteTag { tint: false })
            {
                return;
            }

            var x = letterIndex / waveSize;
            x = AMath.FixFloat(x);

            // Calculate delay progress, the delay is applied over one cycle
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * frequency);

            // Calculate cycle progress and apply limit, the limit is applied over one cycle
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var rainbowColor = Color.HSVToRGB((x + time * frequency) % 1, 1, 1);
            var finalColor = Color.Lerp(Color.white, rainbowColor, finalProgress);
            result.MultiplyColor(finalColor);
        }

        public override bool animatesColor => true;
        public override bool animatesTransform => false;
    }

    public class RainbowParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "rainbow";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var rainbow = new Rainbow(tag);

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                rainbow.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                rainbow.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                rainbow.waveSize = waveSize;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                rainbow.limit = limit;
            }

            return rainbow;
        }
    }
}