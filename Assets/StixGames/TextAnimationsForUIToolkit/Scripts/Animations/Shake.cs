using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Shake : TextAnimation, IAmplitude
    {
        public Shake(): this("#external")
        {
        }

        public Shake(string creatorTag)
            : base(creatorTag) { }

        [SerializeField]
        private float _amplitude = 0.13f;

        public float amplitude 
        { 
            get => _amplitude; 
            set => _amplitude = value; 
        }

        public float delay = float.NegativeInfinity;

        public float frequency = 8f;

        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            Random.InitState(letterIndex + 1652);

            var noiseY = time * frequency;

            // Calculate delay progress
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * 1.6f);

            // Calculate cycle progress and apply limit, the limit is in seconds here
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = time - delayEndOrStart;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit) * 1.6f);
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var x1 = Random.Range(-10000f, 10000f);
            var y1 = noiseY;
            var x2 = Random.Range(-10000f, 10000f);
            var y2 = noiseY;

            var xOffset = Mathf.PerlinNoise(x1, y1) * 2 - 1;
            var yOffset = Mathf.PerlinNoise(x2, y2) * 2 - 1;

            var offset = new Vector2(xOffset, yOffset);
            offset = offset.normalized * Mathf.Pow(offset.magnitude, 0.3f);

            result.AddHorizontalOffset(offset.x * amplitude * finalProgress * 0.8f);
            result.AddVerticalOffset(offset.y * amplitude * finalProgress);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;
    }

    public class ShakeParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "shake";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var shake = new Shake(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                shake.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                shake.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                shake.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                shake.limit = limit;
            }

            return shake;
        }
    }
}
