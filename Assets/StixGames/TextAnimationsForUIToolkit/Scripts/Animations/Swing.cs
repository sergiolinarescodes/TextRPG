using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Swing : TextAnimation, IAmplitude, ISerializationCallbackReceiver
    {
        public Swing(): this("#external")
        {
        }

        public Swing(string creatorTag)
            : base(creatorTag) 
        { 
            amplitude = 25f;
            frequency = 0.8f;
            waveSize = float.PositiveInfinity;
            delay = float.NegativeInfinity;
        }

        [IncludeInSnapshotTest]
        [SerializeField]
        private float _amplitude;
        private float _convertedAmplitude;
        
        public float amplitude
        {
            get => _amplitude;
            set
            {
                _amplitude = value;
                _convertedAmplitude = value * Mathf.Deg2Rad;
            }
        }

        [SerializeField]
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

            // Calculate cycle progress and apply limit, it is smoothed over one cycle duration
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            var y = Mathf.Sin(x + time * _convertedFrequency) * _convertedAmplitude * finalProgress;
            result.AddRotation(y);
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
            _convertedAmplitude = _amplitude * Mathf.Deg2Rad;
            _convertedFrequency = _frequency * AMath.Tau;
            _convertedWaveSize = _waveSize / AMath.Tau;
        }
    }

    public class SwingParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "swing";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var swing = new Swing(tag);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                swing.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                swing.delay = delay;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                swing.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                swing.waveSize = waveSize;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                swing.limit = limit;
            }

            return swing;
        }
    }
}
