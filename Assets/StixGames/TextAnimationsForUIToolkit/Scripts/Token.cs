using System;

namespace TextAnimationsForUIToolkit
{
    internal enum TokenType
    {
        Invalid,
        Word,
        Whitespace,
        LAngleBracket,
        RAngleBracket,
        Equal,
        Slash,
        Backslash,
        SingleQuote,
        DoubleQuote,
        Newline
    }

    internal class Token
    {
        [IncludeInSnapshotTest]
        public TokenType type { get; internal set; }

        /// <summary>
        /// The text used only for word and whitespace tokens
        /// </summary>
        [IncludeInSnapshotTest]
        public string text { get; internal set; }

        internal bool isValid => type != TokenType.Invalid;

        public bool IsSingleSpaceWhitespace()
        {
            if (type != TokenType.Whitespace)
            {
                return false;
            }

            return text == " ";
        }

        public override string ToString()
        {
            return type switch
            {
                TokenType.Word or TokenType.Whitespace => text,
                TokenType.LAngleBracket => "<",
                TokenType.RAngleBracket => ">",
                TokenType.Equal => "=",
                TokenType.Slash => "/",
                TokenType.SingleQuote => "'",
                TokenType.DoubleQuote => "\"",
                TokenType.Backslash => "\\",
                TokenType.Newline => "\n",
                var type
                    => throw new ArgumentOutOfRangeException(
                        nameof(this.type),
                        type,
                        "Unknown token type"
                    )
            };
        }

        #region Helpers
        public static Token Word(string text)
        {
            return new Token { type = TokenType.Word, text = text };
        }

        public static Token Whitespace(string text)
        {
            return new Token { type = TokenType.Whitespace, text = text };
        }

        public static Token lAngleBracket => new() { type = TokenType.LAngleBracket };

        public static Token rAngleBracket => new() { type = TokenType.RAngleBracket };

        public static Token equal => new() { type = TokenType.Equal };

        public static Token slash => new() { type = TokenType.Slash };
        public static Token singleQuote => new() { type = TokenType.SingleQuote };
        public static Token doubleQuote => new() { type = TokenType.DoubleQuote };
        public static Token backslash => new() { type = TokenType.Backslash };

        public static Token newline => new() { type = TokenType.Newline };

        public static readonly Token Invalid = new() { type = TokenType.Invalid };

        #endregion

        #region Equals

        private bool Equals(Token other)
        {
            return type == other.type && text == other.text;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Token)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)type, text);
        }

        public static bool operator ==(Token left, Token right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Token left, Token right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
