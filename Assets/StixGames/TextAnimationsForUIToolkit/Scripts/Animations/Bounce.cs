using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Bounce : TextAnimation, IAmplitude
    {
        public Bounce() : this("#external") { }

        public Bounce(string creatorTag)
            : base(creatorTag)
        {
        }

        [SerializeField]
        private float _amplitude = 0.5f;

        public float amplitude 
        { 
            get => _amplitude; 
            set => _amplitude = value; 
        }

        /// <summary>
        /// The frequency of full circles
        /// </summary>
        public float frequency = 1f;

        public float waveSize = 20;

        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var x = letterIndex / waveSize;
            x = AMath.FixFloat(x);

            // Calculate cycle progress and apply limit, the limit is applied over one cycle
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) ;
            var cycleProgress = (time - delayEndOrStart) * frequency + x;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            limitProgress = AMath.FixFloat(limitProgress);

            var bounceProgress = Mathf.Repeat(x + time * frequency, 1);
            var y = RemappedBounce(bounceProgress) * amplitude * limitProgress;

            result.AddVerticalOffset(y);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;

        private float RemappedBounce(float p)
        {
            p = p * (1 + 1 / 2.75f) - 1 / 2.75f;
            return 1 - EaseInBounce(p);
        }

        /// <summary>
        /// The basic browser EaseOutBounce function.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private float EaseInBounce(float p)
        {
            if (p < 1 / 2.75f)
            {
                return 7.5625f * p * p;
            }
            if (p < 2 / 2.75f)
            {
                p -= 1.5f / 2.75f;

                return 7.5625f * p * p + .75f;
            }

            // Remove the tiny bounces
            return 1;

            // if (p < 2.5f / 2.75f)
            // {
            //     p -= 2.25f / 2.75f;
            //
            //     return 7.5625f * p * p + .9375f;
            // }
            //
            // p -= 2.625f / 2.75f;
            //
            // return 7.5625f * p * p + .984375f;
        }
    }

    public class BounceParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "bounce";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var bounce = new Bounce(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                bounce.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                bounce.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                bounce.waveSize = waveSize;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                bounce.limit = limit;
            }

            return bounce;
        }
    }
}
