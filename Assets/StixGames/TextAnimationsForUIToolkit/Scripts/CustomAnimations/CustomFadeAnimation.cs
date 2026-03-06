using System;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Fades;
using UnityEngine;

namespace TextAnimationsForUIToolkit.CustomAnimations
{
    [Serializable]
    public class CustomFadeAnimation : Fade
    {
        public CustomFadeAnimation(
            string creatorTag,
            [NotNull] CustomAnimationPreset preset,
            FadeType fadeType
        )
            : base(creatorTag)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            this.preset = preset;
            _fadeType = fadeType;
        }

        [NotNull]
        public CustomAnimationPreset preset;

        public float amplitude { get; set; } = 1;

        private FadeType _fadeType;
        public override FadeType fadeType => _fadeType;

        protected override void AnimateFade(AnimatableTextUnit unit,
            int letterIndex,
            float progress,
            AnimationResult result)
        {
            var translationX = preset.translateX.Evaluate(progress) * amplitude;
            var translationY = preset.translateY.Evaluate(progress) * amplitude;
            var color = preset.color.Evaluate(progress) * Mathf.Clamp01(amplitude);
            var rotation = preset.rotation.Evaluate(progress) * Mathf.Deg2Rad * amplitude;
            var scale = preset.scaleOffset.Evaluate(progress) * amplitude;

            result.AddHorizontalOffset(translationX);
            result.AddVerticalOffset(translationY);
            result.MultiplyColor(color);
            result.AddRotation(rotation);
            result.AddScaleOffset(scale);
        }

        public override bool animatesColor => true;
        public override bool animatesTransform => true;
    }
}
