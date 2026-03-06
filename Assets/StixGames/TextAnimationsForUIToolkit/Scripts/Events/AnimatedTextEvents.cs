using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit.Events
{
    public class AnimatedTextEvents : MonoBehaviour
    {
        public UIDocument uiDocument;

        [Tooltip(
            "The name (ID) of the animated text element. The animated text element must exit in the UI Document when this component is enabled."
        )]
        public string animatedTextElementName;
        public float minTimeBetweenLetterEvents;

        public UnityEvent onLetterAppeared;

        [FormerlySerializedAs("onTypewritingFinished")]
        public UnityEvent onAppearanceFinished;
        public UnityEvent onVanishingFinished;

        [SerializeField]
        private CustomEventHandler[] customEvents;

        private IAnimatedTextElement _animatedTextElement;

        private float _lastLetterEvent;

        private readonly Dictionary<string, UnityEvent> _customEventHandlers = new();

        private void Awake()
        {
            foreach (var customEvent in customEvents)
            {
                try
                {
                    AddCustomEventHandler(customEvent.eventName, customEvent.onCustomEvent);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        private void OnEnable()
        {
            var animatedTextElement = TextAnimationUtility.GetAnimatedTextElement(
                uiDocument,
                animatedTextElementName,
                this
            );

            if (animatedTextElement == null)
            {
                return;
            }

            _animatedTextElement = animatedTextElement;
            _animatedTextElement.animationEvent += OnAnimationEvent;
        }

        public void AddCustomEventHandler(string eventName, UnityEvent handler)
        {
            var wasSuccessful = _customEventHandlers.TryAdd(eventName, handler);
            if (!wasSuccessful)
            {
                throw new ArgumentException($"A event handler for '{eventName}' already exists.");
            }
        }

        public void RemoveCustomEventHandler(string eventName)
        {
            _customEventHandlers.Remove(eventName);
        }

        private void OnDisable()
        {
            if (_animatedTextElement != null)
            {
                _animatedTextElement.animationEvent -= OnAnimationEvent;
            }
        }

        private void OnAnimationEvent(TextAnimationEvent ev)
        {
            switch (ev)
            {
                case LetterAppearanceEvent:
                    if (Time.time < _lastLetterEvent + minTimeBetweenLetterEvents)
                    {
                        return;
                    }

                    onLetterAppeared?.Invoke();
                    break;
                case TextAppearanceFinishedEvent:
                    onAppearanceFinished?.Invoke();
                    break;
                case TextVanishingFinishedEvent:
                    onVanishingFinished?.Invoke();
                    break;
                case CustomEvent customEvent:
                    if (
                        _customEventHandlers.TryGetValue(
                            customEvent.name,
                            out var customEventHandler
                        )
                    )
                    {
                        customEventHandler.Invoke();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ev));
            }
        }

        [Serializable]
        public class CustomEventHandler
        {
            public string eventName;
            public UnityEvent onCustomEvent;
        }
    }
}
