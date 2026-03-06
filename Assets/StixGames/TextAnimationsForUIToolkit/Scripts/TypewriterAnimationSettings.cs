using System.Collections.Generic;
using System.Linq;
using TextAnimationsForUIToolkit.CustomAnimations;
using TextAnimationsForUIToolkit.Fades;
using UnityEngine;

namespace TextAnimationsForUIToolkit
{
    [CreateAssetMenu(
        menuName = "Stix Games/Text Animations for UI Toolkit/Typewriter Animation Settings",
        fileName = "Animation Settings",
        order = 1
    )]
    public class TypewriterAnimationSettings : ScriptableObject
    {
        #region Fade In
        public bool useFadeIn;

        public FadeIn fadeIn = new();

        public bool useOffsetIn;

        public OffsetFadeIn offsetIn = new();

        public bool useRandomDirectionIn;

        public RandomDirFadeIn randomDirectionIn = new();

        public bool useSizeIn;

        public SizeFadeIn sizeIn = new();
        #endregion

        #region Fade Out
        public bool useFadeOut;

        public FadeOut fadeOut = new();

        public bool useOffsetOut;

        public OffsetFadeOut offsetOut = new();

        public bool useRandomDirectionOut;

        public RandomDirFadeIn randomDirectionOut = new();

        public bool useSizeOut;

        public SizeFadeOut sizeOut = new();
        #endregion

        public List<CustomAnimationPreset> customTypewriterAnimations = new();

        public List<FadeAnimation> GetFadeInAnimations()
        {
            var animations = new List<FadeAnimation>();
            if (useFadeIn)
            {
                animations.Add(fadeIn);
            }

            if (useRandomDirectionIn)
            {
                animations.Add(randomDirectionIn);
            }

            if (useOffsetIn)
            {
                animations.Add(offsetIn);
            }

            if (useSizeIn)
            {
                animations.Add(sizeIn);
            }

            animations.AddRange(
                from preset in customTypewriterAnimations
                where preset.isTextAppearanceEffect
                select new CustomFadeAnimation(null, preset, FadeType.FadeIn)
                {
                    duration = preset.defaultDuration
                }
            );

            return animations;
        }

        public List<FadeAnimation> GetFadeOutAnimations()
        {
            var animations = new List<FadeAnimation>();

            if (useFadeOut)
            {
                animations.Add(fadeOut);
            }

            if (useRandomDirectionOut)
            {
                animations.Add(randomDirectionOut);
            }

            if (useOffsetOut)
            {
                animations.Add(offsetOut);
            }

            if (useSizeOut)
            {
                animations.Add(sizeOut);
            }

            animations.AddRange(
                from preset in customTypewriterAnimations
                where preset.isTextVanishingEffect
                select new CustomFadeAnimation(null, preset, FadeType.FadeOut)
                {
                    duration = preset.defaultDuration
                }
            );

            return animations;
        }
    }
}
