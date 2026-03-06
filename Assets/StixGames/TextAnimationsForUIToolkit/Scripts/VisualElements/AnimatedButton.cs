using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Events;
using TextAnimationsForUIToolkit.Parsing;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace TextAnimationsForUIToolkit
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class AnimatedButton : Button, IAnimatedTextElement
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<AnimatedButton, UxmlTraits>
        {
        }

        public new class UxmlTraits : Button.UxmlTraits
        {
            private readonly UxmlAssetAttributeDescription<TextAnimationSettings> _settings =
                new() { name = "settings" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var animatedButton = (AnimatedButton)ve;
                var settings = _settings.GetValueFromBag(bag, cc);
                animatedButton.Initialize(settings);
            }
        }

        private void Initialize(TextAnimationSettings newSettings)
        {
            settings = newSettings;
        }
#endif

        public AnimatedButton()
        {
            textAnimator = new TextAnimator(this);
            text = "<wave>Animated Button</wave>";

            RegisterCallback<GeometryChangedEvent>(textAnimator.GeometryChanged);
        }

        internal TextAnimator textAnimator;

        public sealed override string text
        {
            get => textAnimator.text;
            set
            {
                enableRichText = true;
                textAnimator.SetText(value);
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public TextAnimationSettings settings
        {
            get => textAnimator.settings;
            set => textAnimator.settings = value;
        }

        public bool isPlaying => textAnimator.isPlaying;

        public bool isAppearing => textAnimator.isAppearing;

        public IReadOnlyList<TextUnit> textUnits => textAnimator.textUnits;

        public void AddAnimationParser(SimpleTextAnimationTagParser parser)
        {
            textAnimator.AddAnimationParser(parser);
        }

        public void RemoveAnimationParser(SimpleTextAnimationTagParser parser)
        {
            textAnimator.RemoveAnimationParser(parser);
        }

        public event Action<TextAnimationEvent> animationEvent
        {
            add => textAnimator.animationEvent += value;
            remove => textAnimator.animationEvent -= value;
        }

        public event Action<LetterAppearanceEvent> letterAppeared
        {
            add => textAnimator.letterAppeared += value;
            remove => textAnimator.letterAppeared -= value;
        }

        public event Action<TextAppearanceFinishedEvent> textAppearanceFinished
        {
            add => textAnimator.textAppearanceFinished += value;
            remove => textAnimator.textAppearanceFinished -= value;
        }

        public event Action<TextVanishingFinishedEvent> textVanishingFinished
        {
            add => textAnimator.textVanishingFinished += value;
            remove => textAnimator.textVanishingFinished -= value;
        }

        public event Action<CustomEvent> customEventTriggered
        {
            add => textAnimator.customEventTriggered += value;
            remove => textAnimator.customEventTriggered -= value;
        }

        public void Play()
        {
            textAnimator.Play();
        }

        public void Pause()
        {
            textAnimator.Pause();
        }

        public void SetTime(float time, bool resetEvents = false)
        {
            textAnimator.SetTime(time, resetEvents);
        }

        public void Skip(bool emitEvents = true)
        {
            textAnimator.Skip(emitEvents);
        }
    }
}