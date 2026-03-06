using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace TextAnimationsForUIToolkit.Fades
{
    [Serializable]
    public abstract class RandomDirFade : Fade
    {
        [FormerlySerializedAs("Amplitude")]
        public float amplitude = 0.8f;

        protected RandomDirFade() { }

        protected RandomDirFade(string creatorTag)
            : base(creatorTag) { }

        protected override void AnimateFade(
            AnimatableTextUnit unit,
            int letterIndex,
            float progress,
            AnimationResult result)
        {
            Random.InitState(letterIndex + 876651239);

            var pos = Random.insideUnitCircle * amplitude * (1 - progress);
            result.AddHorizontalOffset(pos.x);
            result.AddVerticalOffset(pos.y);
        }

        public override bool animatesColor => false;
        public override bool animatesTransform => true;
    }

    [Serializable]
    public class RandomDirFadeIn : RandomDirFade
    {
        public override FadeType fadeType => FadeType.FadeIn;

        public RandomDirFadeIn() { }

        public RandomDirFadeIn(string creatorTag)
            : base(creatorTag) { }
    }

    [Serializable]
    public class RandomDirFadeOut : RandomDirFade
    {
        public override FadeType fadeType => FadeType.FadeOut;

        public RandomDirFadeOut() { }

        public RandomDirFadeOut(string creatorTag)
            : base(creatorTag) { }
    }

    public class RandomDirFadeParser : SimpleTextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            yield return "random-in";
            yield return "random-out";
        }

        public override TextAnimation CreateAnimation(string tag, Parameters parameters)
        {
            RandomDirFade fade = tag switch
            {
                "random-in" => new RandomDirFadeIn(tag),
                "random-out" => new RandomDirFadeOut(tag),
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
