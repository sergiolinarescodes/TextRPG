using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class SizeWave : TextAnimation, IAmplitude, ISerializationCallbackReceiver
    {
        public SizeWave() : this("#external")
        {
        }

        public SizeWave(string creatorTag)
            : base(creatorTag)
        {
            frequency = 0.8f;
            waveSize = 10.0f;
        }

        [SerializeField] private float _amplitude = 0.3f;

        public float amplitude
        {
            get => _amplitude;
            set => _amplitude = value;
        }

        public float delay = float.NegativeInfinity;

        [IncludeInSnapshotTest] [SerializeField]
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

        [IncludeInSnapshotTest] [SerializeField]
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

        public float limit = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var x = letterIndex / _convertedWaveSize;
            x = AMath.FixFloat(x);

            // Calculate delay progress, it is smoothed over one cycle duration
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * _frequency);

            // Calculate cycle progress and apply limit
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var y = Mathf.Sin(x + time * _convertedFrequency) * amplitude * finalProgress;
            result.AddScaleOffset(y);
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

    public class SizeWaveParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "size-wave";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var sizeWave = new SizeWave(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                sizeWave.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                sizeWave.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                sizeWave.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                sizeWave.waveSize = waveSize;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                sizeWave.limit = limit;
            }

            return sizeWave;
        }
    }
}