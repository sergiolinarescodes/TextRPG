using UnityEngine;

namespace TextRPG.Core.Scroll
{
    public sealed record ScrollDefinition(
        string ScrambledWord,
        string OriginalWord,
        int ManaCost,
        Color Color)
    {
        public static readonly Color ScrollPurple = new(0.7f, 0.3f, 1f);
        public string DisplayName => ScrambledWord.ToUpperInvariant();
    }
}
