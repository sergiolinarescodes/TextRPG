using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Wave : TextAnimation, IAmplitude, ISerializationCallbackReceiver
    {
        public Wave(): this("#external")
        {
        }

        public Wave(string creatorTag)
            : base(creatorTag) 
        { 
            frequency = 1.5f;
            waveSize = 10.0f;
        }

        [SerializeField]
        private float _amplitude = 0.15f;

        public float amplitude
        {
            get => _amplitude;
            set => _amplitude = value;
        }

        [SerializeField]
        public float delay = float.NegativeInfinity;

        [IncludeInSnapshotTest]
        [SerializeField]
        private float _frequency;
        private float _convertedFrequency;

        /// <summary>
        /// The frequency of full circles
        /// </summary>
        public float frequency
        {
            get => _frequency;
            set
            {
                _frequency = value;
                _convertedFrequency = value * AMath.Tau;
            }
        }

        [IncludeInSnapshotTest]
        [SerializeField]
        private float _waveSize;
        private float _convertedWaveSize;

        public float waveSize
        {
            get => _waveSize;
            set
            {
                _waveSize = value;
                _convertedWaveSize = value / AMath.Tau;
            }
        }

        [SerializeField]
        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var x = letterIndex / _convertedWaveSize;
            x = AMath.FixFloat(x);

            // Calculate delay progress
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * frequency);

            // Calculate cycle progress and apply limit
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var y = Mathf.Sin(x + time * _convertedFrequency) * amplitude * finalProgress;

            result.AddVerticalOffset(y);
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
            _convertedWaveSize = _waveSize / AMath.Tau;
        }
    }

    public class WaveParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "wave";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var wave = new Wave(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                wave.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                wave.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                wave.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                wave.waveSize = waveSize;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                wave.limit = limit;
            }

            return wave;
        }
    }
}
