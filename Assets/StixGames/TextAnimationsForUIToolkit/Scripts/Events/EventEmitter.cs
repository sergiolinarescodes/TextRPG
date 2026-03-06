using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.TypewriterTags;
using UnityEngine;

namespace TextAnimationsForUIToolkit.Events
{
    internal class EventEmitter
    {
        private int _currentIndex = 0;

        private readonly List<TextAnimationEvent> _events = new();

        [CanBeNull]
        public TextAnimationSettings settings { get; set; }

        public event Action<TextAnimationEvent> animationEvent;

        public EventEmitter(TextAnimationSettings settings)
        {
            this.settings = settings;
        }

        public event Action<TextAppearanceFinishedEvent> textAppearanceFinished;
        public event Action<TextVanishingFinishedEvent> textVanishingFinished;
        public event Action<LetterAppearanceEvent> letterAppeared;
        public event Action<CustomEvent> customEventTriggered;

        public void SetLetters(List<TextUnit> textUnits)
        {
            Clear();

            var appearanceEventTimeOffset =
                settings != null ? settings.appearanceEventTimeOffset : 0;

            float lastLetterAppearance = 0;
            float lastLetterVanishing = 0;

            foreach (var unit in textUnits)
            {
                switch (unit)
                {
                    case Letter letter:
                        var letterAppearance = new LetterAppearanceEvent(
                            letter.appearanceTime + appearanceEventTimeOffset,
                            letter.letter
                        );
                        _events.Add(letterAppearance);

                        if (letter.appearanceTime > lastLetterAppearance)
                        {
                            lastLetterAppearance = letter.appearanceTime;
                        }
                        if (letter.vanishingTime > lastLetterVanishing)
                        {
                            lastLetterVanishing = letter.vanishingTime;
                        }
                        break;
                    case CustomEventTag customEventTag:
                        var customEvent = new CustomEvent(
                            customEventTag.appearanceTime + appearanceEventTimeOffset,
                            customEventTag.name
                        );
                        _events.Add(customEvent);
                        break;
                }
            }

            _events.Add(
                new TextAppearanceFinishedEvent(lastLetterAppearance + appearanceEventTimeOffset)
            );

            _events.Add(new TextVanishingFinishedEvent(lastLetterVanishing));

            _events.Sort();
        }

        public void Update(float time)
        {
            while (_currentIndex < _events.Count && time > _events[_currentIndex].eventTime)
            {
                EmitEvent(_events[_currentIndex]);
                _currentIndex++;
            }
        }

        private void EmitEvent(TextAnimationEvent animationEvent)
        {
            try
            {
                this.animationEvent?.Invoke(animationEvent);

                switch (animationEvent)
                {
                    case LetterAppearanceEvent letterAppearance:
                        letterAppeared?.Invoke(letterAppearance);
                        break;
                    case TextAppearanceFinishedEvent textAppearanceFinished:
                        this.textAppearanceFinished?.Invoke(textAppearanceFinished);
                        break;
                    case TextVanishingFinishedEvent textVanishingFinished:
                        this.textVanishingFinished?.Invoke(textVanishingFinished);
                        break;
                    case CustomEvent customEvent:
                        customEventTriggered?.Invoke(customEvent);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Clear()
        {
            _currentIndex = 0;
            _events.Clear();
        }

        public void SetEventTime(float targetTime)
        {
            var targetIndex = _events.FindIndex(e => e.eventTime >= targetTime);

            // The time is larger than the last event
            if (targetIndex < 0)
            {
                SetAllEventsEmitted();
                return;
            }

            _currentIndex = targetIndex;
        }

        public void SetAllEventsEmitted()
        {
            _currentIndex = _events.Count;
        }
    }
}
