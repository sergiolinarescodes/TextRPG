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
    public class Wiggle : TextAnimation, IAmplitude, ISerializationCallbackReceiver
    {
        public Wiggle(): this("#external")
        {
        }

        public Wiggle(string creatorTag)
            : base(creatorTag) 
        { 
            frequency = 1.5f;
        }

        [SerializeField]
        private float _amplitude = 0.3f;

        public float amplitude
        {
            get => _amplitude;
            set => _amplitude = value;
        }

        public float delay = float.NegativeInfinity;

        [IncludeInSnapshotTest]
        [SerializeField]
        private float _frequency;

        private float _convertedFrequency;

        public float frequency
        {
            get => _frequency;
            set
            {
                _frequency = value;
                _convertedFrequency = value * AMath.Tau;
            }
        }

        private const float AngleOffset = 15f;

        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            Random.InitState(letterIndex);
            var randomTimeOffset = Random.Range(0f, AMath.Tau);
            var randomAngle = Random.Range(90f - AngleOffset, 90f + AngleOffset) * Mathf.Deg2Rad;

            // The delay is smoothed over one cycle of the animation
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * _frequency);

            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var xFraction = Mathf.Cos(randomAngle);
            var yFraction = Mathf.Sin(randomAngle);
            var offset = Mathf.Sin(time * _convertedFrequency + randomTimeOffset) * amplitude * finalProgress;

            result.AddVerticalOffset(offset * yFraction * amplitude);
            result.AddHorizontalOffset(offset * xFraction);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;
        
        public void OnBeforeSerialize()
        {
            // Nothing needed here
        }
        
        public void OnAfterDeserialize()
        {
            // Recalculate computed values after deserialization
            _convertedFrequency = _frequency * AMath.Tau;
        }
    }

    public class WiggleParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "wiggle";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var wiggle = new Wiggle(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                wiggle.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                wiggle.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                wiggle.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                wiggle.limit = limit;
            }

            return wiggle;
        }
    }
}
