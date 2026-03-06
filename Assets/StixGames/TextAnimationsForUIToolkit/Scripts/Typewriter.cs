using System.Collections.Generic;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.TypewriterTags;
using UnityEngine.Profiling;

namespace TextAnimationsForUIToolkit
{
    internal class Typewriter
    {
        public const bool DefaultUseTypewriting = false;
        public const float DefaultTypingSpeed = 40f;
        public const bool DefaultUseTextVanishing = false;
        public const float DefaultPauseAfterComma = 0.3f;
        public const float DefaultPauseAfterPunctuation = 1f;

        [CanBeNull]
        public TextAnimationSettings settings { get; set; }

        private bool useTypewriter =>
            settings != null ? settings.enableTextAppearance : DefaultUseTypewriting;
        private float typingSpeed =>
            settings != null ? settings.baseAppearanceSpeed : DefaultTypingSpeed;

        private bool useTextVanishing =>
            settings != null ? settings.enableTextVanishing : DefaultUseTextVanishing;
        private float vanishingSpeed =>
            settings != null ? settings.baseVanishingSpeed : DefaultTypingSpeed;
        private float vanishingDelay => settings != null ? settings.vanishingDelay : 0;

        private float pauseAfterComma =>
            settings != null ? settings.pauseAfterComma : DefaultPauseAfterComma;
        private float pauseAfterPunctuation =>
            settings != null ? settings.pauseAfterPunctuation : DefaultPauseAfterPunctuation;

        public float latestAppearanceTime { get; private set; } = 0;

        private float _currentTime;
        private float _speedMultiplier = 1;

        public Typewriter(TextAnimationSettings settings)
        {
            this.settings = settings;
        }

        private void Clear()
        {
            latestAppearanceTime = 0;
            ClearControl();
        }

        private void ClearControl()
        {
            _speedMultiplier = 1;
        }

        public void AddTypewriterTimings(List<TextUnit> textUnits)
        {
            Profiler.BeginSample("TextAnimations.Typewriter");
            Clear();
            Appearance(textUnits);
            Disappearance(textUnits);
            Profiler.EndSample();
        }

        private void Appearance(List<TextUnit> textUnits)
        {
            if (!useTypewriter)
            {
                return;
            }

            ClearControl();

            _currentTime = 0f;
            var timePerLetter = 1.0f / typingSpeed;

            foreach (var data in textUnits)
            {
                switch (data)
                {
                    case AnimatableTextUnit unit:
                        _currentTime += timePerLetter / _speedMultiplier;
                        unit.appearanceTime = _currentTime;

                        // I'm changing the appearance time like this, in case I ever add text removal (backspace)
                        if (_currentTime > latestAppearanceTime)
                        {
                            latestAppearanceTime = _currentTime;
                        }

                        if (unit is Letter { isWordEnd: true } letter)
                        {
                            foreach (var wordLetter in letter.word)
                            {
                                wordLetter.wordAppearanceTime = _currentTime;
                            }

                            // Only add punctuation pauses for the last punctuation is a word,
                            // otherwise text like `...` will cause excessive delays
                            _currentTime += AddPunctuationPauses(letter);
                        }
                        break;
                    case TimedTextUnit timedTextUnit:
                        timedTextUnit.appearanceTime = _currentTime;
                        break;
                    case TypewriterTag controlTag:
                        HandleTypewriterTag(controlTag);
                        break;
                }
            }
        }

        private void Disappearance(List<TextUnit> textUnits)
        {
            if (!useTextVanishing)
            {
                return;
            }

            ClearControl();

            _currentTime = vanishingDelay;
            var timePerLetter = 1.0f / vanishingSpeed;

            foreach (var data in textUnits)
            {
                switch (data)
                {
                    case AnimatableTextUnit unit:
                        _currentTime += timePerLetter / _speedMultiplier;
                        unit.vanishingTime = _currentTime;

                        if (unit is Letter { isWordEnd: true } letter)
                        {
                            foreach (var wordLetter in letter.word)
                            {
                                wordLetter.wordVanishingTime = _currentTime;
                            }

                            _currentTime += AddPunctuationPauses(letter);
                        }

                        break;
                    case TimedTextUnit timedTextUnit:
                        timedTextUnit.vanishingTime = _currentTime;
                        break;
                    case TypewriterTag controlTag:
                        HandleTypewriterTag(controlTag);
                        break;
                }
            }
        }

        private void HandleTypewriterTag(TypewriterTag typewriterTag)
        {
            switch (typewriterTag)
            {
                case OpenAnimationSpeedModifier speedModifier:
                    _speedMultiplier = speedModifier.speed;
                    break;
                case CloseAnimationSpeedModifier:
                    _speedMultiplier = 1;
                    break;
                case PauseTypewriterTag pauseTag:
                    _currentTime += pauseTag.pause;
                    break;
            }
        }

        private float AddPunctuationPauses(Letter letter)
        {
            return letter.letter switch
            {
                ',' => pauseAfterComma,
                '.' or '?' or '!' => pauseAfterPunctuation,
                _ => 0
            };
        }
    }
}
