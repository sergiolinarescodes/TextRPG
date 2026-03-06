using UnityEngine;

namespace TextAnimationsForUIToolkit.CustomAnimations
{
    [CreateAssetMenu(
        fileName = "Custom Animation Preset",
        menuName = "Stix Games/Text Animations for UI Toolkit/Custom Animation Preset",
        order = 2
    )]
    public class CustomAnimationPreset : ScriptableObject
    {
        public string tag = "custom";

        [Tooltip("The number of times the animation will play per second.")]
        public float defaultFrequency = 1f;

        [Tooltip(
            "The animation will be slightly delayed after each letter. It repeats after 'Wave Size' letters. You can set it to 0 to remove the wave effect. Negative values are allowed."
        )]
        public float defaultWaveSize = 40;

        [Tooltip("The duration of the text vanishing or appearance effect.")]
        public float defaultDuration = 2;

        public AnimationCurve translateX = AnimationCurve.Constant(0, 1, 0);
        public AnimationCurve translateY = AnimationCurve.Constant(0, 1, 0);

        public bool animateColor = true;
        public Gradient color;

        [Tooltip("The rotation in degrees. Values can be positive or negative.")]
        public AnimationCurve rotation = AnimationCurve.Constant(0, 1, 0);
        public AnimationCurve scaleOffset = AnimationCurve.Constant(0, 1, 0);

        public bool isTextAppearanceEffect;
        public bool isTextVanishingEffect;

        [Tooltip("If true, this animation always animates the color of <sprite> tags, even if tint is not set to 1.")]
        public bool forceAnimateSpriteColor;
    }
}
