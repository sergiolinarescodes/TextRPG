using System.Collections.Generic;
using System.Linq;
using TextAnimationsForUIToolkit.Fades;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine;

namespace TextAnimationsForUIToolkit.CustomAnimations
{
    public class CustomAnimationParser : TextAnimationTagParser
    {
        public override IEnumerable<string> GetTagNames()
        {
            if (settings == null)
            {
                return Enumerable.Empty<string>();
            }

            foreach (var animation in settings.customAnimations)
            {
                if (!IsValidName(animation.tag))
                {
                    throw new CustomAnimationException(
                        $"The name '{animation.tag}' is not a valid custom animation name."
                    );
                }
            }

            return settings.customAnimations.Select(x => x.tag);
        }

        public static bool IsValidName(string tag)
        {
            return tag.All(IsValidChar);
        }

        private static bool IsValidChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '-';
        }

        public override IEnumerable<TextAnimation> CreateAnimations(
            string tag,
            Parameters parameters
        )
        {
            var preset = settings.customAnimations.Find(x => x.tag == tag);
            if (preset == null)
            {
                Debug.LogError("The custom animation with the tag '{tag}' could not be found.");
                yield break;
            }

            if (preset.isTextAppearanceEffect)
            {
                yield return CreateFadeAnimation(tag, preset, parameters, FadeType.FadeIn);
            }

            if (preset.isTextVanishingEffect)
            {
                yield return CreateFadeAnimation(tag, preset, parameters, FadeType.FadeOut);
            }

            if (!preset.isTextAppearanceEffect && !preset.isTextVanishingEffect)
            {
                yield return CreateAnimation(tag, preset, parameters);
            }
        }

        private CustomAnimation CreateAnimation(
            string tag,
            CustomAnimationPreset preset,
            Parameters parameters
        )
        {
            var customAnimation = new CustomAnimation(tag, preset);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                customAnimation.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var delay))
            {
                customAnimation.delay = delay;
            }

            if (parameters.TryGetFloatValue("l", out var limit))
            {
                customAnimation.limit = limit;
            }

            if (parameters.TryGetFloatValue("f", out var frequency))
            {
                customAnimation.frequency = frequency;
            }
            else
            {
                customAnimation.frequency = preset.defaultFrequency;
            }

            if (parameters.TryGetFloatValue("w", out var waveSize))
            {
                customAnimation.waveSize = waveSize;
            }
            else
            {
                customAnimation.waveSize = preset.defaultWaveSize;
            }

            return customAnimation;
        }

        private CustomFadeAnimation CreateFadeAnimation(
            string tag,
            CustomAnimationPreset preset,
            Parameters parameters,
            FadeType fadeType
        )
        {
            var customAnimation = new CustomFadeAnimation(tag, preset, fadeType);

            if (parameters.TryGetFloatValue("a", out var amplitude))
            {
                customAnimation.amplitude = amplitude;
            }

            if (parameters.TryGetFloatValue("d", out var duration))
            {
                customAnimation.duration = duration;
            }
            else
            {
                customAnimation.duration = preset.defaultDuration;
            }

            return customAnimation;
        }

        public override bool HasDynamicTags => true;
    }
}
