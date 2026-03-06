using System;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Animations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Utility;
using UnityEngine;

namespace TextAnimationsForUIToolkit.CustomAnimations
{
    [Serializable]
    public class CustomAnimation : TextAnimation, IAmplitude
    {
        public CustomAnimation(string creatorTag, [NotNull] CustomAnimationPreset preset)
            : base(creatorTag)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            this.preset = preset;
        }

        [NotNull] public CustomAnimationPreset preset;

        public float amplitude { get; set; } = 1;

        public float delay { get; set; } = float.NegativeInfinity;
        public float frequency { get; set; }

        public float waveSize { get; set; }

        public float limit { get; set; } = float.PositiveInfinity;

        public override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var x = letterIndex / waveSize;
            x = AMath.FixFloat(x);

            var progress = Mathf.Repeat(x + time * frequency, 1);
            progress = AMath.FixFloat(progress);

            var translationX = preset.translateX.Evaluate(progress) * amplitude;
            var translationY = preset.translateY.Evaluate(progress) * amplitude;
            var rotation = preset.rotation.Evaluate(progress) * Mathf.Deg2Rad * amplitude;
            var scale = preset.scaleOffset.Evaluate(progress) * amplitude;

            // Calculate delay progress, it is smoothed over one cycle duration
            var delayEnd = AMath.FixFloat(unit.appearanceTime) + delay;
            var delayProgress = Mathf.Clamp01((time - delayEnd) * frequency);

            // Calculate cycle progress and apply limit, it is smoothed over one cycle duration
            var delayEndOrStart = AMath.FixFloat(unit.appearanceTime) + AMath.FixFloat(delay);
            var cycleProgress = (time - delayEndOrStart) * frequency;
            var limitProgress = Mathf.Clamp01(1 - (cycleProgress - limit));
            var finalProgress = delayProgress * limitProgress;
            finalProgress = AMath.FixFloat(finalProgress);

            translationX = Mathf.Lerp(0, translationX, finalProgress);
            translationY = Mathf.Lerp(0, translationY, finalProgress);
            rotation = Mathf.Lerp(0, rotation, finalProgress);
            scale = Mathf.Lerp(0, scale, finalProgress);

            result.AddHorizontalOffset(translationX);
            result.AddVerticalOffset(translationY);
            result.AddRotation(rotation);
            result.AddScaleOffset(scale);

            if (preset.animateColor && (unit.allowAnimatingColor || preset.forceAnimateSpriteColor))
            {
                var color = Color.Lerp(Color.white, preset.color.Evaluate(progress), amplitude);
                color = Color.Lerp(Color.white, color, finalProgress);
                result.MultiplyColor(color);
            }
        }

        public override bool animatesColor => preset.animateColor;
        public override bool animatesTransform => true;
    }
}