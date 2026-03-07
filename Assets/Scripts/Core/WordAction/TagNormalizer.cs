namespace TextRPG.Core.WordAction
{
    internal static class TagNormalizer
    {
        internal static string Normalize(string tag)
            => string.IsNullOrWhiteSpace(tag) ? "" : tag.Trim().ToUpperInvariant().Replace(' ', '_');
    }
}
