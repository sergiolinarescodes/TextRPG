using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Animations
{
    [Serializable]
    public class Rotate : TextAnimation, ISerializationCallbackReceiver
    {
        public Rotate(): this("#external")
        {
        }

        public Rotate(string creatorTag)
            : base(creatorTag)
        {
            frequency = -0.5f;
            waveSize = 60.0f;
        }

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

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var x = -letterIndex / _convertedWaveSize;
            x = AMath.FixFloat(x);

            result.AddRotation(x + _convertedFrequency * time);
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

    public class RotateParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "rot";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            var rotate = new Rotate(tag);

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                rotate.frequency = frequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                rotate.waveSize = waveSize;
            }

            return rotate;
        }
    }
}
