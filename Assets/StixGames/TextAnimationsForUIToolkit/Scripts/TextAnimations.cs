using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Events;
using TextAnimationsForUIToolkit.Parsing;
using TextAnimationsForUIToolkit.Renderer;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    internal class TextAnimator
    {
        private TextAnimationSettings _settings;

        public TextAnimationSettings settings
        {
            get => _settings;
            set
            {
                _settings = value;
                _preprocessor.settings = value;
                _typewriter.settings = value;
                _parser.settings = value;
                _eventEmitter.settings = value;
                renderer.settings = value;

                SetText(text);
            }
        }

        private TextAnimationSettings _previousSettings;

        public string text { get; private set; }

        public IReadOnlyList<TextUnit> textUnits => _parser.textUnits;

        private readonly TextPreprocessor _preprocessor;

        [NotNull]
        private readonly TextTokenizer _tokenizer = new();

        [NotNull]
        private readonly TextParser _parser;

        [NotNull]
        private readonly Typewriter _typewriter;

        [NotNull]
        private readonly EventEmitter _eventEmitter;

        [NotNull]
        internal readonly AnimationRenderer renderer;

        private readonly VisualElement _parent;

        private float _animationTime;
        private float _latestAppearanceTime;

        private float _lastUpdateTime;

        private IVisualElementScheduledItem _updateSchedule;
        private long _currentFrameTimeMillis;

        private bool _isDebugAnimating;

        private float targetFrameTimeSeconds =>
            _settings != null ? _settings.targetFrameTime : 1f / 30f;
        private long targetFrameTimeMillis => (long)(targetFrameTimeSeconds * 1000f);

        private int lastUpdate = -1;

        private readonly ResolvedStyleData _resolvedStyleData = new();

        public bool isPlaying { get; private set; } = true;

        public TextAnimator(VisualElement parent)
        {
            _parent = parent;
            _preprocessor = new TextPreprocessor(settings);
            _typewriter = new Typewriter(settings);
            _parser = new TextParser(settings);
            _eventEmitter = new EventEmitter(settings);
            renderer = new AnimationRenderer(parent, settings);
            renderer.geometryChanged += GeometryChanged;
        }

        public bool isAppearing => _animationTime < _latestAppearanceTime;

        public void SetText(string value, bool alwaysRender = false)
        {
            value ??= "";

            if (text == value && settings == _previousSettings)
            {
                return;
            }

            _previousSettings = settings;

            Profiler.BeginSample("TextAnimations.SetText");

            text = value;
            _lastUpdateTime = -1;
            value = _preprocessor.Process(value);
            _tokenizer.Tokenize(value);
            _parser.Parse(_tokenizer.tokens);
            _typewriter.AddTypewriterTimings(_parser.textUnits);
            _latestAppearanceTime = _typewriter.latestAppearanceTime;
            _eventEmitter.SetLetters(_parser.textUnits);

            // Always execute SetLetters, even in edit mode, so errors can be seen without entering play mode
            renderer.SetTextUnits(_parser.textUnits);

            var animationDebuggingEnabled = settings != null && settings.enableAnimationDebugging;
            if (!Application.isPlaying && !alwaysRender && !animationDebuggingEnabled)
            {
                renderer.SetUnmodifiedText(value);
            }

            ResetAnimation();

            Profiler.EndSample();
        }

        public void AddAnimationParser(SimpleTextAnimationTagParser parser)
        {
            _parser.AddAnimationParser(parser);
        }

        /// <summary>
        /// Removes parsers from all tags supported by this parser, without checking if this is actually the parser in use.
        /// </summary>
        /// <param name="parser"></param>
        public void RemoveAnimationParser(SimpleTextAnimationTagParser parser)
        {
            _parser.RemoveAnimationParser(parser);
        }

        public static void AddGlobalAnimationParser(SimpleTextAnimationTagParser parser)
        {
            TextParser.AddGlobalAnimationParser(parser);
        }

        /// <summary>
        /// Removes parsers from all tags supported by this parser, without checking if this is actually the parser in use.
        /// </summary>
        /// <param name="parser"></param>
        public static void RemoveGlobalAnimationParser(SimpleTextAnimationTagParser parser)
        {
            TextParser.RemoveGlobalAnimationParser(parser);
        }

        public static void ClearGlobalAnimationParsers()
        {
            TextParser.ClearGlobalAnimationParsers();
        }

        public event Action<TextAnimationEvent> animationEvent
        {
            add => _eventEmitter.animationEvent += value;
            remove => _eventEmitter.animationEvent -= value;
        }

        public event Action<LetterAppearanceEvent> letterAppeared
        {
            add => _eventEmitter.letterAppeared += value;
            remove => _eventEmitter.letterAppeared -= value;
        }

        public event Action<TextAppearanceFinishedEvent> textAppearanceFinished
        {
            add => _eventEmitter.textAppearanceFinished += value;
            remove => _eventEmitter.textAppearanceFinished -= value;
        }

        public event Action<TextVanishingFinishedEvent> textVanishingFinished
        {
            add => _eventEmitter.textVanishingFinished += value;
            remove => _eventEmitter.textVanishingFinished -= value;
        }

        public event Action<CustomEvent> customEventTriggered
        {
            add => _eventEmitter.customEventTriggered += value;
            remove => _eventEmitter.customEventTriggered -= value;
        }

        public void GeometryChanged(GeometryChangedEvent evt)
        {
            if (_updateSchedule == null)
            {
                _updateSchedule = _parent.schedule.Execute(Update);
                SetFrameTime(targetFrameTimeMillis);
                _updateSchedule.StartingIn(0);
            }

            Update();
        }

        private void SetFrameTime(long frameTimeMillis)
        {
            _updateSchedule.Every(frameTimeMillis);
            _currentFrameTimeMillis = frameTimeMillis;
        }

        private void ResetAnimation()
        {
            _animationTime = 0;
            _lastUpdateTime = Time.time;
        }

        private void Update()
        {
            if (lastUpdate == Time.frameCount)
            {
                return;
            }
            lastUpdate = Time.frameCount;

            if (_currentFrameTimeMillis != targetFrameTimeMillis)
            {
                SetFrameTime(targetFrameTimeMillis);
            }

            if (settings != null && settings.enableAnimationDebugging)
            {
                _isDebugAnimating = true;
                SetText(text, true);
                SetTime(settings.debugAnimationTime);
                return;
            }

            if (_isDebugAnimating)
            {
                renderer.SetUnmodifiedText(text);
                _isDebugAnimating = false;
            }

            var wasRendered = RenderText();

            if (isPlaying)
            {
                _animationTime += Time.time - _lastUpdateTime;
                _lastUpdateTime = Time.time;
            }
            else
            {
                _lastUpdateTime = Time.time;
                return;
            }

            // If the text can't be rendered (e.g. because the VisualElement is disabled) don't emit the update event
            if (!wasRendered)
            {
                return;
            }

            _eventEmitter.Update(_animationTime);
        }

        private bool RenderText()
        {
            var resolvedStyle = _parent.resolvedStyle;
            if (
                resolvedStyle.display == DisplayStyle.None
                || resolvedStyle.visibility == Visibility.Hidden
            )
            {
                return false;
            }

            _resolvedStyleData.fontSize = resolvedStyle.fontSize;
            _resolvedStyleData.unityTextAlign = resolvedStyle.unityTextAlign;
            _resolvedStyleData.whiteSpace = resolvedStyle.whiteSpace;
            _resolvedStyleData.fontAscent = resolvedStyle.unityFont?.ascent
                                            ?? resolvedStyle.unityFontDefinition.font?.ascent ?? 0;
            _resolvedStyleData.fontBaseSize = resolvedStyle.unityFont?.fontSize
                                            ?? resolvedStyle.unityFontDefinition.font?.fontSize ?? 0;

            #if UNITY_6000_3_OR_NEWER
            _resolvedStyleData.unityMaterial = resolvedStyle.unityMaterial.material;
            #endif

            renderer.Update(_animationTime, _resolvedStyleData);

            return true;
        }

        public void Play()
        {
            isPlaying = true;
        }

        public void Pause()
        {
            isPlaying = false;
        }

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
        public void SetTime(float time, bool resetEvents = false)
        {
            _animationTime = time;
            if (resetEvents)
            {
                _eventEmitter.SetEventTime(time);
            }
            RenderText();
        }

        public void Skip(bool emitEvents = true)
        {
            // Why do things this complicated instead of setting the animation time to a high value?
            // This way there's no jumping in the animations.
            // An alternative would be a hard coded value in the renderer to ignore all fades,
            // but this way no special handling is necessary.
            // I might change this in the future, in case it turns out that this method causes performance issues,
            // or overcomplicates things.

            _latestAppearanceTime = float.NegativeInfinity;

            foreach (
                var unit in _parser.textUnits.Select(x => x as TimedTextUnit).Where(x => x != null)
            )
            {
                unit.appearanceTime = float.NegativeInfinity;
                unit.vanishingTime = float.PositiveInfinity;
            }

            // Emit all remaining events
            if (emitEvents)
            {
                _eventEmitter.Update(float.PositiveInfinity);
            }
            else
            {
                _eventEmitter.SetAllEventsEmitted();
            }
        }
    }
}
