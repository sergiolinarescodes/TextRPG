using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using TextAnimationsForUIToolkit.Animations;
using TextAnimationsForUIToolkit.BuiltinTags;
using TextAnimationsForUIToolkit.CustomAnimations;
using TextAnimationsForUIToolkit.Data;
using TextAnimationsForUIToolkit.Fades;
using TextAnimationsForUIToolkit.TypewriterTags;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace TextAnimationsForUIToolkit.Parsing
{
    internal class TextParser
    {
        private static readonly HashSet<string> BuiltinRichTextTags =
            new()
            {
                "a",
                "align",
                "allcaps",
                "b",
                "br",
                "cspace",
                "font",
                "font-weight",
                "gradient",
                "i",
                "indent",
                "line-height",
                "line-indent",
                "lowercase",
                "margin",
                "margin",
                "mark",
                "mspace",
                "nobr",
                "pos",
                "rotate",
                "s",
                "smallcaps",
                "space",
                "style",
                "sub",
                "u",
                "uppercase",
                "voffset",
                "width"
            };

        private static readonly HashSet<string> StackableRichTextTags = new();

        public TextParser(TextAnimationSettings settings)
        {
            this.settings = settings;

            AddAnimationParser(new WaveParser());
            AddAnimationParser(new WiggleParser());
            AddAnimationParser(new RotateParser());
            AddAnimationParser(new SwingParser());
            AddAnimationParser(new ShakeParser());
            AddAnimationParser(new RainbowParser());
            AddAnimationParser(new BounceParser());
            AddAnimationParser(new SizeWaveParser());

            AddAnimationParser(new CustomAnimationParser());

            AddAnimationParser(new FadeParser());
            AddAnimationParser(new SizeFadeParser());
            AddAnimationParser(new RandomDirFadeParser());
            AddAnimationParser(new OffsetFadeParser());

            AddTagParser(new SpeedControlTagParser());
            AddTagParser(new PauseControlTagParser());
            AddTagParser(new CustomEventParser());
        }

        internal List<TextUnit> textUnits { get; } = new();

        private readonly Dictionary<string, ITagParser> _parsers = new();
        private static readonly Dictionary<string, ITagParser> _globalParsers = new();

        private readonly List<ITagParser> _dynamicParsers = new();
        private static readonly List<ITagParser> _globalDynamicParsers = new();

        private bool disableTagParsing = false;

        public void AddAnimationParser(TextAnimationTagParser parser)
        {
            AddTagParser(parser);
        }

        /// <summary>
        /// Removes parsers from all tags supported by this parser, without checking if this is actually the parser in use.
        /// </summary>
        /// <param name="parser"></param>
        public void RemoveAnimationParser(TextAnimationTagParser parser)
        {
            RemoveTagParser(parser);
        }

        public static void AddGlobalAnimationParser(TextAnimationTagParser parser)
        {
            if (parser.HasDynamicTags)
            {
                _globalDynamicParsers.Add(parser);
                return;
            }

            foreach (var name in parser.GetTagNames())
            {
                if (!_globalParsers.TryAdd(name, parser))
                {
                    Debug.LogError($"Duplicate animation parser for tag: {name}");
                }
            }
        }

        public static void RemoveGlobalAnimationParser(TextAnimationTagParser parser)
        {
            foreach (var name in parser.GetTagNames())
            {
                _globalParsers.Remove(name);
            }
        }

        public static void ClearGlobalAnimationParsers()
        {
            _globalParsers.Clear();
        }

        private void AddTagParser(ITagParser parser)
        {
            if (parser.HasDynamicTags)
            {
                _dynamicParsers.Add(parser);
                return;
            }

            parser.settings = settings;
            foreach (var name in parser.GetTagNames())
            {
                if (!_parsers.TryAdd(name, parser))
                {
                    Debug.LogError($"Duplicate animation parser for tag: {name}");
                }
            }
        }

        private void RemoveTagParser(ITagParser parser)
        {
            foreach (var name in parser.GetTagNames())
            {
                _parsers.Remove(name);
            }
        }

        private int index { get; set; }

        private List<Token> tokens { get; set; }

        /// <summary>
        /// The animations currently applied to Letters's.
        ///
        /// Make sure to clone this list before making changes, since all DisplayUnits hold references this list.
        /// </summary>
        private List<TextAnimation> _animations = new();

        /// <summary>
        /// The rich text tags currently applied to Letters's.
        ///
        /// Make sure to clone this list before making changes, since all DisplayUnits hold references this list.
        /// </summary>
        private List<RichTextTag> _richTextTags = new();

        private List<Color> _colors = new();
        private float? _alpha;

        private List<SizeTag> _sizes = new();

        private readonly List<ShaderPropertyTag> _shaderProperties = new();

        private readonly StringBuilder _tagValueBuilder = new();
        private readonly StringBuilder _rawStringBuilder = new();

        private TextAnimationSettings _settings;
        private readonly StringBuilder _wordText = new();
        private List<Letter> _wordLetters = new();

        public TextAnimationSettings settings
        {
            get => _settings;
            set
            {
                _settings = value;
                foreach (var parser in _parsers)
                {
                    parser.Value.settings = value;
                }
            }
        }

        private void Next()
        {
            index++;
        }

        private void JumpBy(int offset)
        {
            index += offset;
        }

        private Token GetToken(int offset = 0)
        {
            var i = index + offset;

            if (i < 0 || i >= tokens.Count)
            {
                return Token.Invalid;
            }

            return tokens[i];
        }

        internal void Parse(List<Token> tokens)
        {
            Profiler.BeginSample("TextAnimations.Parse");

            Clear();
            this.tokens = tokens;

            while (index < this.tokens.Count)
            {
                switch (GetToken().type)
                {
                    case TokenType.RAngleBracket:
                    case TokenType.Equal:
                    case TokenType.Slash:
                    case TokenType.SingleQuote:
                    case TokenType.DoubleQuote:
                    case TokenType.Backslash:
                    case TokenType.Word:
                        Word();
                        break;
                    case TokenType.Whitespace:
                        Whitespace();
                        break;
                    case TokenType.Newline:
                        Newline();
                        break;
                    case TokenType.LAngleBracket:
                        var succeeded = Tag();
                        if (!succeeded)
                        {
                            SpecialCharacterOutsideTag();
                        }

                        break;
                    case TokenType.Invalid:
                        throw new FormatException("Invalid token");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Assign the word to the last letters
            AssignWordToLetters();

            Profiler.EndSample();
        }

        private void Clear()
        {
            index = 0;
            textUnits.Clear();
            _colors.Clear();
            _alpha = null;
            _animations.Clear();
            _richTextTags.Clear();
            _rawStringBuilder.Clear();
            _tagValueBuilder.Clear();
            _wordText.Clear();
            _shaderProperties.Clear();
        }

        private bool Tag()
        {
            if (GetToken().type != TokenType.LAngleBracket)
            {
                throw new ParserException("Tag called when not on an angle bracket");
            }

            var offset = 1;
            if (GetToken(offset).type == TokenType.Slash)
            {
                offset++;
                return ClosingTag(offset);
            }
            else
            {
                return OpenTag(offset);
            }
        }

        private bool OpenTag(int offset)
        {
            if (disableTagParsing)
            {
                return false;
            }

            var nameToken = GetToken(offset);
            if (nameToken.type != TokenType.Word)
            {
                return false;
            }

            offset++;

            var parameters = new Parameters();

            // Color shortcut tag <#FF0000>
            var name = nameToken.text;
            if (name.StartsWith("#"))
            {
                parameters.AddMainParameter(name, name);
                name = "color";
            }

            // main parameter like <alpha=#FF>
            if (GetToken(offset).type == TokenType.Equal)
            {
                offset++;

                var wasSuccessful = TryParameterValue(
                    ref offset,
                    out var mainParameter,
                    out var rawMainParameter
                );
                if (!wasSuccessful)
                {
                    return false;
                }

                parameters.AddMainParameter(mainParameter, rawMainParameter);
            }

            IgnoreSingleWhitespace(ref offset);

            if (GetToken(offset).type == TokenType.Word)
            {
                var succeeded = TagParameters(ref offset, parameters);
                if (!succeeded)
                {
                    return false;
                }
            }

            if (GetToken(offset).type != TokenType.RAngleBracket)
            {
                // Tag close expected
                return false;
            }

            offset++;

            var wasHandled = HandleTagOpened(name.ToLower(), parameters);

            if (!wasHandled)
            {
                wasHandled = AddRichTextTag(name, parameters);
            }

            JumpBy(offset);

            return true;
        }

        private bool TagParameters(ref int offset, Parameters parameters)
        {
            while (GetToken(offset).type == TokenType.Word)
            {
                var name = GetToken(offset);
                if (name.type != TokenType.Word)
                {
                    return false;
                }

                offset++;

                if (GetToken(offset).type != TokenType.Equal)
                {
                    return false;
                }

                offset++;

                var wasSuccessful = TryParameterValue(
                    ref offset,
                    out var stringValue,
                    out var rawStringValue
                );
                if (!wasSuccessful)
                {
                    return false;
                }

                parameters.Add(name.text, stringValue, rawStringValue);

                IgnoreSingleWhitespace(ref offset);
            }

            return true;
        }

        private bool TryParameterValue(
            ref int offset,
            out string stringValue,
            out string rawStringValue
        )
        {
            var token = GetToken(offset);
            stringValue = null;
            rawStringValue = null;
            if (token.type == TokenType.Word)
            {
                offset++;
                stringValue = token.text;
                rawStringValue = token.text;
                return true;
            }

            _tagValueBuilder.Clear();
            _rawStringBuilder.Clear();

            bool hasDoubleQuotes;
            if (token.type == TokenType.SingleQuote)
            {
                hasDoubleQuotes = false;
            }
            else if (token.type == TokenType.DoubleQuote)
            {
                hasDoubleQuotes = true;
            }
            else
            {
                return false;
            }

            _rawStringBuilder.Append(token);

            offset++;

            var hasBackslash = false;
            token = GetToken(offset);
            while (token.isValid)
            {
                _rawStringBuilder.Append(token);

                switch (token.type)
                {
                    case TokenType.Backslash when !hasBackslash:
                        hasBackslash = true;
                        break;
                    case TokenType.SingleQuote when !hasBackslash && !hasDoubleQuotes:
                    case TokenType.DoubleQuote when !hasBackslash && hasDoubleQuotes:
                        // The string was finished
                        offset++;
                        stringValue = _tagValueBuilder.ToString();
                        rawStringValue = _rawStringBuilder.ToString();
                        return true;
                    default:
                        hasBackslash = false;
                        _tagValueBuilder.Append(token);
                        break;
                }

                offset++;
                token = GetToken(offset);
            }

            return false;
        }

        private bool ClosingTag(int offset)
        {
            var name = GetToken(offset);
            if (name.type != TokenType.Word)
            {
                return false;
            }

            offset++;

            if (disableTagParsing && name.text != "noparse")
            {
                return false;
            }

            IgnoreSingleWhitespace(ref offset);

            if (GetToken(offset).type != TokenType.RAngleBracket)
            {
                return false;
            }

            offset++;

            var wasAnimation = HandleTagClosed(name.text);
            if (!wasAnimation)
            {
                RemoveRichTextTag(name.text);
            }

            JumpBy(offset);
            return true;
        }

        private void IgnoreSingleWhitespace(ref int offset)
        {
            if (GetToken(offset).IsSingleSpaceWhitespace())
            {
                offset++;
            }
        }

        private void Word()
        {
            _rawStringBuilder.Clear();

            var offset = 0;
            var token = GetToken(offset);
            while (
                token.isValid
                && token.type
                    is not TokenType.Whitespace
                    and not TokenType.Newline
                    and not TokenType.LAngleBracket
            )
            {
                _rawStringBuilder.Append(token);
                offset++;
                token = GetToken(offset);
            }

            var word = _rawStringBuilder.ToString();
            _wordText.Append(word);
            foreach (var letter in word)
            {
                var committedLetter = AddAnimatedLetter(letter);
                committedLetter.letter = letter;
                _wordLetters.Add(committedLetter);
            }

            JumpBy(offset);
        }

        private void SpecialCharacterOutsideTag()
        {
            var text = GetToken().ToString();

            if (text.Length is 0 or > 1)
            {
                throw new ParserException(
                    "Special character with more than one character can't be converted"
                );
            }

            var animatedLetter = AddAnimatedLetter(text[0]);
            animatedLetter.isWordStart = true;
            animatedLetter.isWordEnd = true;
            animatedLetter.word = new[] { animatedLetter };
            Next();
        }

        private void Whitespace()
        {
            AssignWordToLetters();

            var whitespace = new Whitespace();
            whitespace.text += GetToken().text;
            whitespace.richTextTags = _richTextTags;
            whitespace.hasLink = _richTextTags.Any(x => x.tag == "a");
            textUnits.Add(whitespace);
            Next();
        }

        private void Newline()
        {
            AssignWordToLetters();

            var newline = new Newline();
            textUnits.Add(newline);
            Next();
        }

        private void AssignWordToLetters()
        {
            if (_wordLetters.Count == 0)
            {
                return;
            }

            var wordText = _wordText.ToString();
            foreach (var letter in _wordLetters)
            {
                letter.wordText = wordText;
                letter.word = _wordLetters;
            }

            _wordLetters.First().isWordStart = true;
            _wordLetters.Last().isWordEnd = true;

            _wordLetters = new List<Letter>();
            _wordText.Clear();
        }

        private Letter AddAnimatedLetter(char letterChar)
        {
            var letter = new Letter();
            letter.letter += letterChar;
            letter.isWordStart = false;
            letter.isWordEnd = false;
            ApplyAnimatableProperties(letter);
            textUnits.Add(letter);
            return letter;
        }

        private bool TryGetTagParser(string tag, out ITagParser parser)
        {
            var wasFound = _parsers.TryGetValue(tag, out parser);

            if (wasFound)
            {
                return true;
            }

            wasFound = _globalParsers.TryGetValue(tag, out parser);
            if (wasFound)
            {
                return true;
            }

            var dynamicParser = _dynamicParsers.Find(parser =>
            {
                parser.settings = settings;
                return parser.GetTagNames().Contains(tag);
            });
            if (dynamicParser is not null)
            {
                parser = dynamicParser;
                return true;
            }

            dynamicParser = _globalDynamicParsers.Find(parser =>
            {
                parser.settings = settings;
                return parser.GetTagNames().Contains(tag);
            });
            if (dynamicParser is not null)
            {
                parser = dynamicParser;
                return true;
            }

            return false;
        }

        private bool HandleTagOpened(string tag, Parameters parameters)
        {
            switch (tag)
            {
                case "color":
                    return AddColorTag(parameters);
                case "alpha":
                    return AddAlphaTag(parameters);
                case "size":
                    return AddSizeTag(parameters);
                case "sprite":
                    return AddSpriteTag(parameters);
                case "shader":
                    return AddShaderTag(parameters);
                case "noparse":
                    // <noparse> disables parsing of all rich text tags
                    disableTagParsing = true;
                    return true;
            }

            if (!TryGetTagParser(tag, out var parser))
            {
                return false;
            }

            return parser switch
            {
                TextAnimationTagParser animationParser
                    => HandleAnimationTagOpened(tag, parameters, animationParser),
                ControlTagParser controlTagParser
                    => HandleControlTagParserOpened(tag, parameters, controlTagParser),
                _ => throw new ArgumentException("Unknown tag type")
            };
        }

        private bool AddColorTag(Parameters parameters)
        {
            if (!parameters.TryGetMainColorValue(out var color))
            {
                return false;
            }

            _colors = _colors.ToList();
            _colors.Add(color);
            _alpha = null;
            return true;
        }

        private bool AddAlphaTag(Parameters parameters)
        {
            if (parameters.mainParameter is not { Length: 3 } || parameters.mainParameter[0] != '#')
            {
                return false;
            }

            var alpha = Convert.ToByte(parameters.mainParameter.Substring(1, 2), 16);
            _alpha = alpha / 255f;
            return true;
        }

        private void CloseColorTag()
        {
            if (_colors.Any())
            {
                _colors.RemoveAt(_colors.Count - 1);
            }

            _alpha = null;
        }

        private bool AddSizeTag(Parameters parameters)
        {
            SizeTag sizeTag;
            if (!parameters.TryGetMainValue(out var value))
            {
                return false;
            }

            bool TryParseFontSize(string value, out SizeTag sizeTag, bool canBePercent)
            {
                sizeTag = null;

                if (value.EndsWith("em"))
                {
                    if (Parameters.TryParseFloat(value[..^2], out var emValue))
                    {
                        sizeTag = new FractionalSizeTag(emValue);
                        return true;
                    }
                    return false;
                }

                if (value.EndsWith("px"))
                {
                    if (Parameters.TryParseFloat(value[..^2], out var pxValue))
                    {
                        sizeTag = new AbsoluteSizeTag(pxValue);
                        return true;
                    }
                    return false;
                }

                if (Parameters.TryParseFloat(value, out var unmarkedValue))
                {
                    sizeTag = new AbsoluteSizeTag(unmarkedValue);
                    return true;
                }

                if (canBePercent && Parameters.TryParseFloat(value, out var percentValue, true))
                {
                    sizeTag = new FractionalSizeTag(percentValue);
                    return true;
                }

                return false;
            }

            var isOffset = value.StartsWith("+") || value.StartsWith("-");
            if (isOffset && TryParseFontSize(value, out var offsetSizeTag, false))
            {
                offsetSizeTag.isRelative = true;
                sizeTag = offsetSizeTag;
            } else if (TryParseFontSize(value, out var regularSizeTag, true))
            {
                sizeTag = regularSizeTag;
            }
            else
            {
                return false;
            }

            _sizes = _sizes.ToList();
            _sizes.Add(sizeTag);
            return true;
        }

        private void CloseSizeTag()
        {
            if (_sizes.Any())
            {
                _sizes.RemoveAt(_sizes.Count - 1);
            }
        }

        private bool AddShaderTag(Parameters parameters)
        {
            if (!parameters.TryGetValue("property", out var propertyName))
            {
                return false;
            }

            var propertyId = Shader.PropertyToID(propertyName);

            var tag = new ShaderPropertyTag
            {
                ShaderPropertyId = propertyId
            };

            if (parameters.TryGetFloatValue("value", out var floatValue))
            {
                tag.FloatValue = floatValue;
            }
            else if (parameters.TryGetColorValue("value", out var colorValue))
            {
                tag.ColorValue = colorValue;
            }
            else
            {
                return false;
            }

            _shaderProperties.Add(tag);
            return true;
        }

        private void CloseShaderTag()
        {
            if (_shaderProperties.Any())
            {
                _shaderProperties.RemoveAt(_shaderProperties.Count - 1);
            }
        }

        private bool AddSpriteTag(Parameters parameters)
        {
            // Create a new sprite tag
            var spriteTag = new SpriteTag();

            // Process the sprite parameters
            if (parameters.TryGetMainIntValue(out var indexValue) || parameters.TryGetIntValue("index", out indexValue))
            {
                spriteTag.index = index;
            }
            else if (parameters.TryGetMainValue(out var nameValue) ||
                     parameters.TryGetValue("name", out nameValue))
            {
                spriteTag.name = nameValue;
            }

            if (parameters.TryGetColorValue("color", out var colorValue))
            {
                spriteTag.spriteColor = colorValue;
            }

            if (parameters.TryGetValue("tint", out var tintValue))
            {
                spriteTag.tint = tintValue == "1";
            }

            // Before adding sprite, close any current word
            AssignWordToLetters();

            // Apply shared properties from current context
            ApplyAnimatableProperties(spriteTag);

            // Add to text units
            textUnits.Add(spriteTag);

            return true;
        }

        private void ApplyAnimatableProperties(AnimatableTextUnit unit)
        {
            unit.animations = _animations;

            unit.richTextTags = _richTextTags;
            unit.sizeTag = _sizes.Any() ? _sizes.Last() : null;
            unit.hasLink = _richTextTags.Any(x => x.tag == "a");

            #if UNITY_6000_3_OR_NEWER
            unit.shaderProperties = GetCurrentShaderProperties();
            #endif

            if (_colors.Any())
            {
                unit.color = _colors.Last();
            }

            if (_alpha.HasValue)
            {
                unit.opacity = _alpha.Value;
            }
        }

        private List<ShaderPropertyTag> GetCurrentShaderProperties()
        {
            using var propertiesGuard = HashSetPool<int>.Get(out var properties);

            var list =  new List<ShaderPropertyTag>();
            for (var i = _shaderProperties.Count -1; i >= 0; i--)
            {
                var property = _shaderProperties[i];

                // Discard any shader properties that were already set, only the latest value is used.
                if (!properties.Add(property.ShaderPropertyId))
                {
                    continue;
                }

                list.Add(property);
            }
            return list;
        }

        private bool HandleControlTagParserOpened(
            string tag,
            Parameters parameters,
            ControlTagParser parser
        )
        {
            try
            {
                var controlTag = parser.OpenTag(tag, parameters);
                if (controlTag == null)
                {
                    return false;
                }

                textUnits.Add(controlTag);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private bool HandleAnimationTagOpened(
            string tag,
            Parameters parameters,
            TextAnimationTagParser parser
        )
        {
            IEnumerable<TextAnimation> animations;
            try
            {
                animations = parser.CreateAnimations(tag, parameters);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            var didAddAnimation = false;
            foreach (var animation in animations)
            {
                didAddAnimation = true;
                AddAnimation(animation);
            }

            return didAddAnimation;
        }

        private void AddAnimation(TextAnimation animation)
        {
            var newAnimations = _animations.ToList();
            newAnimations.Add(animation);
            _animations = newAnimations;
        }

        private bool HandleTagClosed(string tag)
        {
            switch (tag)
            {
                case "color":
                    CloseColorTag();
                    return true;
                case "size":
                    CloseSizeTag();
                    return true;
                case "shader":
                    CloseShaderTag();
                    return true;
                case "noparse":
                    // </noparse> re-enables parsing of tags
                    disableTagParsing = false;
                    return true;
            }

            if (!TryGetTagParser(tag, out var parser))
            {
                return false;
            }

            return parser switch
            {
                TextAnimationTagParser => RemoveAnimation(tag),
                ControlTagParser controlTagParser
                    => HandleControlTagParserClosed(tag, controlTagParser),
                _ => throw new ArgumentException("Unknown tag type")
            };
        }

        private bool HandleControlTagParserClosed(string tag, ControlTagParser parser)
        {
            try
            {
                var controlTag = parser.CloseTag(tag);
                if (controlTag == null)
                {
                    return false;
                }

                textUnits.Add(controlTag);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private bool RemoveAnimation(string tag)
        {
            if (_animations.Count == 0)
            {
                return false;
            }

            _animations = _animations.ToList();

            var index = _animations.FindLastIndex(x => x.creatorTag == tag);

            if (index < 0)
            {
                return false;
            }

            _animations.RemoveAt(index);

            return true;
        }

        private bool AddRichTextTag(string name, Parameters parameters)
        {
            if (!BuiltinRichTextTags.Contains(name))
            {
                return false;
            }

            _richTextTags = _richTextTags.ToList();

            if (!StackableRichTextTags.Contains(name))
            {
                RemoveRichTextTag(name, false);
            }

            _richTextTags.Add(new RichTextTag(name, parameters));

            return true;
        }

        private void RemoveRichTextTag(string tag, bool createNewList = true)
        {
            if (_richTextTags.Count == 0)
            {
                return;
            }

            if (createNewList)
            {
                _richTextTags = _richTextTags.ToList();
            }

            var index = _richTextTags.FindLastIndex(x => x.tag == tag);

            if (index >= 0)
            {
                _richTextTags.RemoveAt(index);
            }
        }
    }

    public class ParserException : Exception
    {
        public ParserException()
        {
        }

        protected ParserException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ParserException(string message)
            : base(message)
        {
        }

        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}