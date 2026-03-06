using System;
using System.Collections.Generic;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Events;
using TextAnimationsForUIToolkit.Parsing;

namespace TextAnimationsForUIToolkit
{
    public interface IAnimatedTextElement
    {
        public string text { get; set; }

        public TextAnimationSettings settings { get; set; }

        public bool isAppearing { get; }

        /// <summary>
        /// The internal representation of letters in the text.
        ///
        /// Can be used to dynamically change animations for advanced effects and interactive UI.
        /// </summary>
        public IReadOnlyList<TextUnit> textUnits { get; }

        public void AddAnimationParser(SimpleTextAnimationTagParser parser);
        public void RemoveAnimationParser(SimpleTextAnimationTagParser parser);
        public event Action<TextAnimationEvent> animationEvent;

        public event Action<LetterAppearanceEvent> letterAppeared;
        public event Action<TextAppearanceFinishedEvent> textAppearanceFinished;
        public event Action<TextVanishingFinishedEvent> textVanishingFinished;
        public event Action<CustomEvent> customEventTriggered;

        public bool isPlaying { get; }

        public void Play();

        public void Pause();

        /// <summary>
        /// Sets the animation time.
        /// <p>
        /// If <c>resetEvents</c> is set, events before this timeframe will be set as "emitted" and events after
        /// will be set as "not emitted".
        /// Otherwise, events will be left as they are, so events that were not yet emitted that are now in the past
        /// will be emitted immediately.
        /// </p>
        ///
        /// <p>
        /// Be careful, calling this function will always cause the text to be re-rendered, regardless of framerate
        /// settings.
        /// </p>
        /// </summary>
        /// <param name="resetEvents"></param>
        public void SetTime(float time, bool resetEvents = false);

        /// <summary>
        /// Skip all appearance and disappearance effects.
        /// Internally this function sets appearance times infinitely far in the past and disappearance times
        /// infinitely far in the future.
        /// </summary>
        public void Skip(bool emitEvents = true);
    }
}
