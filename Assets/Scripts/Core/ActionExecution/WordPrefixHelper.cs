namespace TextRPG.Core.ActionExecution
{
    internal static class WordPrefixHelper
    {
        public static bool TryStripGivePrefix(ref string word)
        {
            if (!word.StartsWith("give ")) return false;
            word = word.Substring(5).Trim();
            return word.Length > 0;
        }
    }
}
