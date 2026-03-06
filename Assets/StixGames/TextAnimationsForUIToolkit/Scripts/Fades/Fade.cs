using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Fades
{
    public enum FadeType
    {
        FadeIn,
        FadeOut
    }

    [Serializable]
    public abstract class Fade : FadeAnimation
    {
        protected Fade() { }

        protected Fade(string creatorTag)
            : base(creatorTag) { }

        public abstract FadeType fadeType { get; }

        public sealed override void Animate(
            AnimatableTextUnit unit,
            int letterIndex,
            float time,
            AnimationResult result)
        {
            var fadeStartTime = FadeStartTime(unit);
            var progress = Mathf.Clamp01((time - fadeStartTime) / duration);
            if (float.IsNaN(progress))
            {
                progress = 0;
            }

            switch (fadeType)
            {
                case FadeType.FadeIn:
                    // Nothing to do here
                    break;
                case FadeType.FadeOut:
                    progress = 1 - progress;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            AnimateFade(unit, letterIndex, progress, result);
        }

        private float FadeStartTime(AnimatableTextUnit unit)
        {
            return fadeType switch
            {
                FadeType.FadeIn => unit.appearanceTime,
                FadeType.FadeOut => unit.vanishingTime - duration,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        protected virtual void AnimateFade(
            AnimatableTextUnit unit,
            int letterIndex,
            float progress,
            AnimationResult result)
        {
            // Animate opacity, regardless of if this is a sprite or not
            result.MultiplyOpacity(progress);
        }

        public override bool isFadeIn => fadeType == FadeType.FadeIn;
        public override bool isFadeOut => fadeType == FadeType.FadeOut;
    }

    [Serializable]
    public class FadeIn : Fade
    {
        public FadeIn() { }

        public FadeIn(string creatorTag)
            : base(creatorTag) { }

        public override FadeType fadeType => FadeType.FadeIn;

        public override bool animatesColor => false;
        public override bool animatesTransform => false;
    }

    [Serializable]
    public class FadeOut : Fade
    {
        public FadeOut() { }

        public FadeOut(string creatorTag)
            : base(creatorTag) { }

        public override FadeType fadeType => FadeType.FadeOut;

        public override bool animatesColor => false;
        public override bool animatesTransform => false;
    }

    public class FadeParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "fade-in";
            yield return "fade-out";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            Fade fade = tag switch
            {
                "fade-in" => new FadeIn(tag),
                "fade-out" => new FadeOut(tag),
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };

            if (parameters.TryGetFloatValue("d", out var duration))
            {
                fade.duration = duration;
            }

            return fade;
        }
    }
}
