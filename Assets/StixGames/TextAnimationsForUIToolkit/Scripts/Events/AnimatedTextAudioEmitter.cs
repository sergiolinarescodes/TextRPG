using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace TextAnimationsForUIToolkit.Events
{
    public enum AudioSequenceType
    {
        Random,
        Sequential
    }

    public class AnimatedTextAudioEmitter : MonoBehaviour
    {
        public AudioSource source;

        public UIDocument uiDocument;

        [Tooltip(
            "The name (ID) of the animated text element. The animated text element must exit in the UI Document when this component is enabled."
        )]
        public string animatedTextElementName;

        public float minTimeBetweenSounds = 0.2f;

        public bool interruptLastSound;

        public AudioSequenceType sequence;

        public List<AudioClip> sounds;

        private IAnimatedTextElement _animatedTextElement;
        private int _soundIndex;
        private float _lastSound = float.NegativeInfinity;

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
            _animatedTextElement.letterAppeared += OnAnimationEvent;
        }

        private void OnDisable()
        {
            if (_animatedTextElement != null)
            {
                _animatedTextElement.letterAppeared -= OnAnimationEvent;
            }
        }

        private void OnAnimationEvent(LetterAppearanceEvent ev)
        {
            if (source == null)
            {
                Debug.LogError("Source is missing", this);
            }

            if (!interruptLastSound && source.isPlaying)
            {
                return;
            }

            if (_lastSound + minTimeBetweenSounds > Time.time)
            {
                return;
            }

            int index;
            switch (sequence)
            {
                case AudioSequenceType.Random:
                    index = Random.Range(0, sounds.Count);
                    break;
                case AudioSequenceType.Sequential:
                    index = _soundIndex;
                    _soundIndex = (_soundIndex + 1) % sounds.Count;

                    // If the index is larger than the count of sounds (should only happen when 0 sounds are set)
                    // return early.
                    if (index >= sounds.Count)
                    {
                        return;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            source.clip = sounds[index];
            source.Play();
            _lastSound = Time.time;
        }
    }
}
