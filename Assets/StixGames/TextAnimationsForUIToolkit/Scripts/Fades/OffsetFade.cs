using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine;
using UnityEngine.Serialization;

namespace TextAnimationsForUIToolkit.Fades
{
    [Serializable]
    public abstract class OffsetFade : Fade
    {
        [FormerlySerializedAs("X")]
        public float x = 0.5f;

        [FormerlySerializedAs("Y")]
        public float y = 0.5f;

        protected OffsetFade() { }

        protected OffsetFade(string creatorTag)
            : base(creatorTag) { }

        protected override void AnimateFade(
            AnimatableTextUnit unit,
            int letterIndex,
            float progress,
            AnimationResult result)
        {
            var pos = new Vector2(x, y) * (1 - progress);
            result.AddHorizontalOffset(pos.x);
            result.AddVerticalOffset(pos.y);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;
    }

    [Serializable]
    public class OffsetFadeIn : OffsetFade
    {
        public override FadeType fadeType => FadeType.FadeIn;

        public OffsetFadeIn() { }

        public OffsetFadeIn(string creatorTag)
            : base(creatorTag) { }
    }

    [Serializable]
    public class OffsetFadeOut : OffsetFade
    {
        public override FadeType fadeType => FadeType.FadeOut;

        public OffsetFadeOut() { }

        public OffsetFadeOut(string creatorTag)
            : base(creatorTag) { }
    }

    public class OffsetFadeParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "offset-in";
            yield return "offset-out";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            OffsetFade fade = tag switch
            {
                "offset-in" => new OffsetFadeIn(tag),
                "offset-out" => new OffsetFadeOut(tag),
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };

            if (parameters.TryGetFloatValue("x", out var x))
            {
                fade.x = x;
            }

            if (parameters.TryGetFloatValue("y", out var y))
            {
                fade.y = y;
            }

            if (parameters.TryGetFloatValue("d", out var duration))
            {
                fade.duration = duration;
            }

            return fade;
        }
    }
}
