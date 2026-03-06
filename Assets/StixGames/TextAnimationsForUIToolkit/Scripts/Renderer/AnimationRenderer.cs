using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Fades;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace TextAnimationsForUIToolkit.Renderer
{
    internal class AnimationRenderer
    {
        public TextAnimationSettings settings { get; set; }

        public event Action<GeometryChangedEvent> geometryChanged;

        private readonly List<AnimatableTextUnit> _letters = new();
        private readonly List<LineData> _lines = new();

        private readonly List<VisualElement> _activeWords = new();
        private readonly Stack<VisualElement> _wordPool = new();

        private readonly List<Label> _activeLetters = new();
        private readonly Stack<Label> _letterPool = new();

        private VisualElement _parent;
        private float _currentFontSize = -1;
        private TextAnchor _currentTextAlign = TextAnchor.UpperLeft;
        private WhiteSpace _currentWhiteSpace = WhiteSpace.Normal;

        private bool _renderFadeIn;
        private bool _renderFadeOut;

        [CanBeNull]
        private List<FadeAnimation> _defaultFadeIns;

        private List<FadeAnimation> _defaultFadeOuts;
        private List<FadeAnimation> _fallbackFadeIns;
        private List<FadeAnimation> _fallbackFadeOuts;
        private List<TextAnimation> _defaultAnimations;

        private VisualElement _container;
        private LineData _currentLine;
        private VisualElement _currentWord;

        private Label _editModeLabel;

        private readonly AnimationResult _result = new();
        private readonly StringBuilder _builder = new();
        private bool _wasJustCreated;

#if UNITY_6000_3_OR_NEWER
        private Material _currentMaterial;
        private static readonly int AppearanceTimeProperty = Shader.PropertyToID("_AppearanceTime");
        private static readonly int VanishingTimeProperty = Shader.PropertyToID("_VanishingTime");
        private static readonly int AnimationTimeProperty = Shader.PropertyToID("_AnimationTime");
#endif

        public AnimationRenderer(VisualElement parent, TextAnimationSettings settings)
        {
            _parent = parent;
            this.settings = settings;
        }

        public void SetTextUnits(List<TextUnit> textUnits)
        {
            Clear();
            _result.Clear();

            _renderFadeIn =
                settings != null ? settings.enableTextAppearance : Typewriter.DefaultUseTypewriting;
            _renderFadeOut =
                settings != null
                    ? settings.enableTextVanishing
                    : Typewriter.DefaultUseTextVanishing;

            if (settings != null)
            {
                _defaultAnimations = settings.defaultAnimations.GetAnimations();

                if (settings.defaultTypewriterAnimationSettings != null)
                {
                    _defaultFadeIns = settings.defaultTypewriterAnimationSettings.GetFadeInAnimations();
                    _defaultFadeOuts = settings.defaultTypewriterAnimationSettings.GetFadeOutAnimations();
                }

                if (settings.fallbackTypewriterAnimationSettings != null)
                {
                    _fallbackFadeIns = settings.fallbackTypewriterAnimationSettings.GetFadeInAnimations();
                    _fallbackFadeOuts = settings.fallbackTypewriterAnimationSettings.GetFadeOutAnimations();
                }
            }

            CreateTextElements(textUnits);
        }

        private void CreateTextElements(List<TextUnit> textUnits)
        {
            Profiler.BeginSample("TextAnimations.CreateTextElements");

            _parent.AddToClassList("unity-label");
            StabilizeSnapshot(_parent);
            _container = new VisualElement();
            StabilizeSnapshot(_container);

            NextLine();

            foreach (var textUnit in textUnits)
            {
                switch (textUnit)
                {
                    case AnimatableTextUnit animatableTextUnit:
                        if (animatableTextUnit is not Letter { isWordStart: false })
                        {
                            _currentWord = null;
                        }

                        if (_currentWord == null)
                        {
                            GetWord();
                        }

                        Debug.Assert(_currentWord != null, nameof(_currentWord) + " != null");

                        _builder.Clear();
                        animatableTextUnit.BuildString(_builder);
                        var letterElement = GetLetterElement();
                        letterElement.name = "animated-text__letter";
                        letterElement.text = _builder.ToString();

                        SetUsageHint(animatableTextUnit, letterElement);
                        SetLink(animatableTextUnit, letterElement);

                        _currentWord.Add(letterElement);
                        animatableTextUnit.label = letterElement;
                        _currentLine.letters.Add(animatableTextUnit);
                        _letters.Add(animatableTextUnit);
                        break;
                    case Newline:
                        NextLine();
                        break;
                    case Whitespace whitespace:
                        if (_currentWord == null)
                        {
                            GetWord();
                        }

                        Debug.Assert(_currentWord != null, nameof(_currentWord) + " != null");

                        _builder.Clear();
                        whitespace.BuildWhitespaceString(_builder);
                        var whitespaceElement = GetLetterElement();
                        whitespaceElement.name = "animated-text__whitespace";
                        whitespaceElement.text = _builder.ToString();
                        whitespaceElement.usageHints = UsageHints.None;
                        _currentWord.Add(whitespaceElement);
                        break;
                }
            }

            // Add the container to the parent, but make it invisible until Update to prevent visible layout changes
            _wasJustCreated = true;
            _container.style.visibility = Visibility.Hidden;
            _parent.Add(_container);

            _container.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            Profiler.EndSample();
        }

        private static void SetUsageHint(AnimatableTextUnit textUnit, Label letterElement)
        {
            var animatesColor = textUnit.animations.Any(x => x.animatesColor);
            var animatesTransform = textUnit.animations.Any(x => x.animatesTransform);
            var usageHint = UsageHints.None;
            if (animatesColor)
            {
                usageHint |= UsageHints.DynamicColor;
            }

            if (animatesTransform)
            {
                usageHint |= UsageHints.DynamicTransform;
            }

            letterElement.usageHints = usageHint;
        }

        private void SetLink(AnimatableTextUnit textUnit, Label letterElement)
        {
            if (textUnit.hasLink && settings != null && settings.linkCursorTexture != null)
            {
                letterElement.style.cursor = new Cursor
                {
                    hotspot = settings.linkCursorHotspot,
                    texture = settings.linkCursorTexture
                };
            }
            else
            {
                letterElement.style.cursor = StyleKeyword.Null;
            }
        }

        internal void Update(float time, ResolvedStyleData style)
        {
            if (_container == null)
            {
                return;
            }

            if (_wasJustCreated)
            {
                _container.style.visibility = StyleKeyword.Null;

                // Not quite sure why this is necessary, but rarely UI Toolkit doesn't update letters while they are
                // invisible, which can cause weird problems with the letter pool.
                foreach (var letter in _activeLetters)
                {
                    letter.MarkDirtyRepaint();
                }
            }

            if (style.unityTextAlign != _currentTextAlign)
            {
                SetTextAlign(style.unityTextAlign);
            }

            if (style.whiteSpace != _currentWhiteSpace)
            {
                foreach (var line in _lines)
                {
                    switch (style.whiteSpace)
                    {
                        case WhiteSpace.NoWrap:
                            line.element.style.flexWrap = Wrap.NoWrap;
                            break;
                        case WhiteSpace.Normal:
                        default:
                            line.element.style.flexWrap = Wrap.Wrap;
                            break;
                    }
                }
            }

            #if UNITY_6000_3_OR_NEWER
            if (style.unityMaterial != _currentMaterial)
            {
                UpdateMaterial(style.unityMaterial);
            }
            #endif

            if (_editModeLabel != null)
            {
                _editModeLabel.style.whiteSpace = style.whiteSpace;
                SetLineAlignment(_container, style.unityTextAlign);
                return;
            }

            Profiler.BeginSample("TextAnimations.UpdateRenderer");

            if (_wasJustCreated || Mathf.Abs(_currentFontSize - style.fontSize) > 0.01f)
            {
                SetFontSize(style.fontSize, style);
            }

            var letterIndex = 0;
            foreach (var letter in _letters)
            {
                UpdateAnimationResult(letter, letterIndex, time);
                ApplyAnimationResult(letter);
                letterIndex++;
            }

            Profiler.EndSample();
        }

#if UNITY_6000_3_OR_NEWER
        private void UpdateMaterial(Material material)
        {
            _currentMaterial = material;

            ClearMaterials();
            if (material == null)
            {
                return;
            }

            var timer = System.Diagnostics.Stopwatch.StartNew();
            foreach (var letter in _letters)
            {
                var letterMaterial = new Material(material);
                letter.material = letterMaterial;
                letter.label.style.unityMaterial = letterMaterial;

                foreach (var p in letter.shaderProperties)
                {
                    if (p.FloatValue.HasValue)
                    {
                        letterMaterial.SetFloat(p.ShaderPropertyId, p.FloatValue.Value);
                    }
                    else if (p.ColorValue.HasValue)
                    {
                        letterMaterial.SetColor(p.ShaderPropertyId, p.ColorValue.Value);
                    }
                }

                letterMaterial.SetFloat(AppearanceTimeProperty, letter.appearanceTime);
                letterMaterial.SetFloat(VanishingTimeProperty, letter.vanishingTime);
            }
        }
#endif

        private void UpdateAnimationResult(AnimatableTextUnit unit, int letterIndex, float time)
        {
            _result.Clear();

            // Do any animations defined with tags
            var hasFadeIn = false;
            var hasFadeOut = false;
            foreach (var animation in unit.animations)
            {
                if (animation is FadeAnimation fade)
                {
                    if (fade.isFadeIn)
                    {
                        if (!_renderFadeIn)
                        {
                            continue;
                        }

                        hasFadeIn = true;
                    }

                    if (fade.isFadeOut)
                    {
                        if (!_renderFadeOut)
                        {
                            continue;
                        }

                        hasFadeOut = true;
                    }
                }

                try
                {
                    animation.Animate(unit, letterIndex, time, _result);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            // Apply default animations and default and fallback fade-in / fade-out animations
            if (settings != null)
            {
                foreach (var animation in _defaultAnimations)
                {
                    try
                    {
                        animation.Animate(unit, letterIndex, time, _result);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                if (_renderFadeIn && _defaultFadeIns != null)
                {
                    foreach (var animation in _defaultFadeIns)
                    {
                        try
                        {
                            animation.Animate(unit, letterIndex, time, _result);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                if (_renderFadeOut && _defaultFadeOuts != null)
                {
                    foreach (var animation in _defaultFadeOuts)
                    {
                        try
                        {
                            animation.Animate(unit, letterIndex, time, _result);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                if (!hasFadeIn && _renderFadeIn && _fallbackFadeIns != null)
                {
                    foreach (var animation in _fallbackFadeIns)
                    {
                        try
                        {
                            animation.Animate(unit, letterIndex, time, _result);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                if (!hasFadeOut && _renderFadeOut && _fallbackFadeOuts != null)
                {
                    foreach (var animation in _fallbackFadeOuts)
                    {
                        try
                        {
                            animation.Animate(unit, letterIndex, time, _result);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

#if UNITY_6000_3_OR_NEWER
            if (unit.material != null)
            {
                unit.material.SetFloat(AnimationTimeProperty, time);
            }
#endif

            // If the letter is outside the appearance and disappearance time, make it invisible.
            if (unit.appearanceTime > time || unit.vanishingTime < time)
            {
                _result.SetInvisible();
            }
        }

        private void ApplyAnimationResult(AnimatableTextUnit unit)
        {
            var element = unit.label;

            var letterFontSize = unit.GetFontSize(_currentFontSize);

            var color = Color.white;
            var hasColor = false;

            // Only apply <color> tags if the text unit allows animating color
            if (unit.color != null && unit.allowAnimatingColor)
            {
                color *= unit.color.Value;
                hasColor = true;
            }

            // Animations handle `unit.allowAnimatingColor` themselves
            if (_result.color != null)
            {
                color *= _result.color.Value;
                hasColor = true;
            }

            if (unit is SpriteTag { spriteColor: not null } spriteTag)
            {
                color *= spriteTag.spriteColor.Value;
                hasColor = true;
            }

            if (hasColor)
            {
                element.style.color = color;
            }
            else
            {
                element.style.color = StyleKeyword.Null;
            }

            var opacity = 1f;
            var hasOpacity = false;

            // Only apply <color> tags if the text unit allows animating color
            if (unit.opacity != null && unit.allowAnimatingColor)
            {
                opacity = unit.opacity.Value;
                hasOpacity = true;
            }

            // Animations handle `unit.allowAnimatingColor` themselves
            if (_result.opacity != null)
            {
                opacity = _result.opacity.Value;
                hasOpacity = true;
            }

            if (hasOpacity)
            {
                element.style.opacity = opacity;
            }
            else
            {
                element.style.opacity = StyleKeyword.Null;
            }

            element.style.translate = new Translate(
                _result.horizontalOffset * letterFontSize,
                -_result.verticalOffset * letterFontSize
            );

            var amount = Mathf.Max(0, 1f + _result.scaleOffset);
            element.style.scale = new Scale(new Vector2(amount, amount));

            if (_result.rotation == null)
            {
                element.style.rotate = Rotate.None();
            }
            else
            {
                element.style.rotate = new Rotate(Angle.Radians(_result.rotation.Value));
            }
        }

        private void SetFontSize(float fontSize, ResolvedStyleData style)
        {
            _currentFontSize = fontSize;
            foreach (var line in _lines)
            {
                if (line.letters.Count == 0)
                {
                    line.element.style.height = _currentFontSize;
                }
                else
                {
                    var maxFontSize = line.letters.Max(x => x.GetFontSize(_currentFontSize));

                    foreach (var letter in line.letters)
                    {
                        ApplyLetterFontSizeOffset(letter, maxFontSize, style);
                    }
                }
            }
        }

        private void ApplyLetterFontSizeOffset(AnimatableTextUnit letter, float maxFontSize, ResolvedStyleData style)
        {
            var element = letter.label;

            var letterFontSize = letter.GetFontSize(_currentFontSize);

            if (letter.sizeTag == null)
            {
                element.style.fontSize = StyleKeyword.Null;
            }
            else
            {
                element.style.fontSize = letterFontSize;
            }

            var ascentFraction = (float)style.fontAscent / style.fontBaseSize;
            var maxToTop = ascentFraction * maxFontSize;
            var toTop = ascentFraction * letterFontSize;
            var diff = maxToTop - toTop;

            if (diff >= 0.01f)
            {
                element.style.marginTop = diff;
            }
            else
            {
                element.style.marginTop = 0;
            }
        }

        private void SetTextAlign(TextAnchor styleUnityTextAlign)
        {
            _currentTextAlign = styleUnityTextAlign;
            SetLineAlignment(_parent, styleUnityTextAlign);
            SetLineAlignment(_container, styleUnityTextAlign);
            foreach (var line in _lines)
            {
                SetLineAlignment(line.element, styleUnityTextAlign);
            }
        }

        private void SetLineAlignment(VisualElement element, TextAnchor textAnchor)
        {
            switch (textAnchor)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    element.style.justifyContent = Justify.FlexStart;
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    element.style.justifyContent = Justify.Center;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    element.style.justifyContent = Justify.FlexEnd;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAnchor), textAnchor, null);
            }

            switch (textAnchor)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                    element.style.alignItems = Align.FlexStart;
                    break;
                case TextAnchor.LowerCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                    element.style.alignItems = Align.Center;
                    break;
                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                    element.style.alignItems = Align.FlexEnd;

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAnchor), textAnchor, null);
            }
        }

        private void NextLine()
        {
            var line = new VisualElement();
            line.name = "animated-text__line";
            line.style.flexDirection = FlexDirection.Row;
            line.style.flexWrap = Wrap.Wrap;
            StabilizeSnapshot(line);
            SetLineAlignment(line, _currentTextAlign);
            _currentLine = new LineData
            {
                element = line,
            };
            _container.Add(line);
            _lines.Add(_currentLine);
        }

        private void GetWord()
        {
            if (!_wordPool.TryPop(out var word))
            {
                word = new VisualElement();
                word.style.flexDirection = FlexDirection.Row;
                word.style.flexShrink = 0;
                StabilizeSnapshot(word);
            }

            _activeWords.Add(word);
            _currentWord = word;
            _currentLine.element.Add(word);
        }

        private Label GetLetterElement()
        {
            if (_letterPool.TryPop(out var label))
            {
                label.style.cursor = StyleKeyword.Null;
                _activeLetters.Add(label);
                return label;
            }

            var newLetter = new Label();
            newLetter.enableRichText = true;
            newLetter.style.flexShrink = 0;
#if UNITY_6000
            newLetter.style.whiteSpace = WhiteSpace.PreWrap;
#else
            newLetter.style.whiteSpace = WhiteSpace.NoWrap;
#endif
            StabilizeSnapshot(newLetter);

            SetMarginPaddingZero(newLetter);

            _activeLetters.Add(newLetter);

            return newLetter;
        }

        public void SetUnmodifiedText(string value)
        {
            if (_editModeLabel != null)
            {
                _editModeLabel.text = value;
            }
            else
            {
                Clear();

                _container = new VisualElement();
                _editModeLabel = new Label(value);
                SetMarginPaddingZero(_editModeLabel);
                _container.Add(_editModeLabel);

                // Add the container to the parent, but make it invisible until Update to prevent visible layout changes
                _wasJustCreated = true;
                _container.style.visibility = Visibility.Hidden;
                _parent.Add(_container);

                _container.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private static void SetMarginPaddingZero(Label newLetter)
        {
            newLetter.style.marginLeft = 0;
            newLetter.style.marginTop = 0;
            newLetter.style.marginRight = 0;
            newLetter.style.marginBottom = 0;
            newLetter.style.paddingLeft = 0;
            newLetter.style.paddingTop = 0;
            newLetter.style.paddingRight = 0;
            newLetter.style.paddingBottom = 0;
        }

        /// <summary>
        /// This function isn't necessary, but it helps make snapshot tests consistent across unity versions.
        /// If you want to optimize a tiny amount of performance, delete everything inside this function.
        /// </summary>
        /// <param name="element"></param>
        private static void StabilizeSnapshot(VisualElement element)
        {
            element.style.scale = Scale.None();
        }

        private void Clear()
        {
            foreach (var letter in _activeLetters)
            {
                _letterPool.Push(letter);
            }

            _activeLetters.Clear();

            foreach (var word in _activeWords)
            {
                word.Clear();
                _wordPool.Push(word);
            }

            _activeWords.Clear();

#if UNITY_6000_3_OR_NEWER
            ClearMaterials();
#endif

            _currentTextAlign = TextAnchor.UpperLeft;
            _currentFontSize = -1;
            _parent.Clear();
            _letters.Clear();
            _lines.Clear();
            _container = null;
            _currentLine = null;
            _currentWord = null;
            _editModeLabel = null;
            _defaultFadeIns = null;
            _defaultFadeOuts = null;
            _fallbackFadeIns = null;
            _fallbackFadeOuts = null;
        }

#if UNITY_6000_3_OR_NEWER
        private void ClearMaterials()
        {
            foreach (var letter in _letters.Where(letter => letter.material != null))
            {
                UnityEngine.Object.Destroy(letter.material);
            }
        }
#endif

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            geometryChanged?.Invoke(evt);
        }

        private class LineData
        {
            public VisualElement element;
            public readonly List<AnimatableTextUnit> letters = new();
        }
    }
}