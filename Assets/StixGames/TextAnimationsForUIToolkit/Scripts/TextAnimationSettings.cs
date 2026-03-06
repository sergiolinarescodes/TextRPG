using System.Collections.Generic;
using TextAnimationsForUIToolkit.CustomAnimations;
using TextAnimationsForUIToolkit.Styles;
using UnityEngine;
using UnityEngine.Serialization;

namespace TextAnimationsForUIToolkit
{
    [CreateAssetMenu(
        menuName = "Stix Games/Text Animations for UI Toolkit/Text Animation Settings",
        fileName = "Text Animation Settings",
        order = 0
    )]
    public class TextAnimationSettings : ScriptableObject
    {
        #region General

        [FormerlySerializedAs("linkCursor")]
        [Tooltip(
            "When using the <noparse><a></noparse> tag, this cursor will be used.\n\nThe texture to use for the cursor style. To use a texture as a cursor, import the texture with \"Read/Write enabled\" in the texture importer (or using the \"Cursor\" defaults)."
        )]
        public Texture2D linkCursorTexture;

        [Tooltip(
            "The offset from the top left of the texture to use as the target point (must be within the bounds of the cursor)."
        )]
        public Vector2 linkCursorHotspot;

        public List<CustomAnimationPreset> customAnimations = new();

        [FormerlySerializedAs("styles")]
        [Tooltip(
            "Each tag defined in this list will be replaced by its opening and closing tags. This way you can easily create more complex tags, for example an <angry> tag that changes text color to red and adds the <shake> animation.\n"
                + "\n"
                + "The replacement happens in a preprocessing step and is done via string replacement.\n"
                + "The replacements happen sequentially, so if the first style contains a tag for the second style, the second style will be replaced as well, but not the other way around."
        )]
        public List<Template> templates = new();

        #endregion

        #region Typewriting and text vanising

        public bool enableTextAppearance = true;

        [Tooltip(
            "The appearance speed in letters per second. The speed can be modified in text with the <speed> tag."
        )]
        public float baseAppearanceSpeed = Typewriter.DefaultTypingSpeed;

        public bool enableTextVanishing;

        [Tooltip(
            "The vanishing speed in letters per second. The speed can be modified in text with the <speed> tag."
        )]
        public float baseVanishingSpeed = Typewriter.DefaultTypingSpeed;

        [Tooltip("The delay in seconds after which the text starts to vanish.")]
        public float vanishingDelay = 10;

        [Tooltip(
            "Adds a pause after each comma in the text, to make text appearance look more naturally."
        )]
        public float pauseAfterComma = Typewriter.DefaultPauseAfterComma;

        [Tooltip(
            "Adds a pause after each punctuation mark (.!?) in the text, to make text appearance look more naturally."
        )]
        public float pauseAfterPunctuation = Typewriter.DefaultPauseAfterPunctuation;

        #endregion

        #region Default animations

        public DefaultAnimations defaultAnimations = new DefaultAnimations();

        #endregion

        #region Default and fallback typewriter animations

        /// <summary>
        /// Fade in and fade out animations that get overlayed over all other effects.
        /// </summary>
        [FormerlySerializedAs("defaultAnimationSettings")]
        [Tooltip(
            "Default animations are always active, including when other text appearance or vanishing tags are set in your text."
        )]
        public TypewriterAnimationSettings defaultTypewriterAnimationSettings;

        /// <summary>
        /// Fade in and fade out animations that are used when no other fade effect is present.
        /// </summary>
        [FormerlySerializedAs("fallbackAnimationSettings")]
        [Tooltip(
            "Fallback animations are active unless appearance or vanishing tags were set in your text."
        )]
        public TypewriterAnimationSettings fallbackTypewriterAnimationSettings;

        #endregion

        #region Events

        [Tooltip(
            "By default letter appearance events are emitted when a letter starts to appear.\n"
                + "Use this setting to offset the appearance events to be more fitting for fade ins."
        )]
        public float appearanceEventTimeOffset;

        #endregion

        #region Performance
        [Min(1)]
        [Tooltip("The maximum framerate the text will be animated with.")]
        public float maxFramerate = 60;

        public float targetFrameTime => 1f / maxFramerate;
        #endregion

        #region Debug

        [Tooltip(
            "When enabled, all animations using this settings object will be paused and set to the debug animation time."
        )]
        public bool enableAnimationDebugging;

        [Tooltip("Debug Animation Time in Seconds")]
        public float debugAnimationTime;

        #endregion
    }
}
