using System.Collections.Generic;
using UnityEngine.Profiling;

namespace TextAnimationsForUIToolkit
{
    internal class TextTokenizer
    {
        public TextTokenizer() { }

        private string text { get; set; }

        public List<Token> tokens { get; } = new();

        private int index { get; set; }

        public void Tokenize(string text)
        {
            Profiler.BeginSample("TextAnimations.Tokenize");
            this.text = text;
            tokens.Clear();
            index = 0;

            // I tried recreating Unity's rich text parser here.
            // As far as I could find, there's no documentation for it, so there's a lot of guesswork involved.

            while (index < this.text.Length)
            {
                switch (this.text[index])
                {
                    case '<':
                        tokens.Add(new Token { type = TokenType.LAngleBracket });
                        IncrementIndex();
                        break;
                    case '>':
                        tokens.Add(new Token { type = TokenType.RAngleBracket });
                        IncrementIndex();
                        break;
                    case '=':
                        tokens.Add(new Token { type = TokenType.Equal });
                        IncrementIndex();
                        break;
                    case '/':
                        tokens.Add(new Token { type = TokenType.Slash });
                        IncrementIndex();
                        break;
                    case '\'':
                        tokens.Add(new Token { type = TokenType.SingleQuote });
                        IncrementIndex();
                        break;
                    case '"':
                        tokens.Add(new Token { type = TokenType.DoubleQuote });
                        IncrementIndex();
                        break;
                    case '\\':
                        tokens.Add(new Token { type = TokenType.Backslash });
                        IncrementIndex();
                        break;
                    case '\r':
                        // Treat \r and \r\n as newline
                        tokens.Add(new Token { type = TokenType.Newline });
                        IncrementIndex();
                        if (this.text[index] == '\n')
                        {
                            IncrementIndex();
                        }
                        break;
                    case '\n':
                        // Treat \r and \r\n as newline
                        tokens.Add(new Token { type = TokenType.Newline });
                        IncrementIndex();
                        break;
                    case var c when char.IsWhiteSpace(c):
                        Whitespace();
                        break;
                    default:
                        Word();
                        break;
                }
            }
            Profiler.EndSample();
        }

        private void Word()
        {
            var startIndex = index;
            while (index < text.Length)
            {
                var c = text[index];
                if (
                    char.IsWhiteSpace(c)
                    || c == '<'
                    || c == '>'
                    || c == '='
                    || c == '/'
                    || c == '\''
                    || c == '"'
                    || c == '\\'
                    || c == '\r'
                    || c == '\n'
                )
                {
                    break;
                }

                IncrementIndex();
            }

            tokens.Add(
                new Token
                {
                    type = TokenType.Word,
                    text = text.Substring(startIndex, index - startIndex)
                }
            );
        }

        private void IncrementIndex()
        {
            index++;
        }

        private void Whitespace()
        {
            var startIndex = index;
            while (index < text.Length)
            {
                if (!char.IsWhiteSpace(text[index]) || text[index] == '\r' || text[index] == '\n')
                {
                    break;
                }

                IncrementIndex();
            }

            tokens.Add(
                new Token
                {
                    type = TokenType.Whitespace,
                    text = text.Substring(startIndex, index - startIndex)
                }
            );
        }
    }
}
