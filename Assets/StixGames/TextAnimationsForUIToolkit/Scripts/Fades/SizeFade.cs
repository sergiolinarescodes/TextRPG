using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine;
using UnityEngine.Serialization;

namespace TextAnimationsForUIToolkit.Fades
{
    [Serializable]
    public abstract class SizeFade : Fade
    {
        [FormerlySerializedAs("Amplitude")]
        public float amplitude;

        protected SizeFade() { }

        protected SizeFade(string creatorTag)
            : base(creatorTag) { }

        protected override void AnimateFade(
            AnimatableTextUnit unit,
            int letterIndex,
            float progress,
            AnimationResult result)
        {
            var size = Mathf.Lerp(amplitude, 1, progress);
            result.AddScaleOffset(size - 1);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;
    }

    [Serializable]
    public class SizeFadeIn : SizeFade
    {
        public override FadeType fadeType => FadeType.FadeIn;

        public SizeFadeIn() { }

        public SizeFadeIn(string creatorTag)
            : base(creatorTag) { }
    }

    [Serializable]
    public class SizeFadeOut : SizeFade
    {
        public override FadeType fadeType => FadeType.FadeOut;

        public SizeFadeOut() { }

        public SizeFadeOut(string creatorTag)
            : base(creatorTag) { }
    }

    public class SizeFadeParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "size-in";
            yield return "size-out";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            SizeFade fade = tag switch
            {
                "size-in" => new SizeFadeIn(tag),
                "size-out" => new SizeFadeOut(tag),
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                fade.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var duration))
            {
                fade.duration = duration;
            }

            return fade;
        }
    }
}
