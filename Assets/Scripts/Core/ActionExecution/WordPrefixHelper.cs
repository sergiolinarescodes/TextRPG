using TextRPG.Core.CombatLoop;

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

        /// <summary>
        /// Shared give-prefix preprocessing: strips prefix, validates, consumes resources.
        /// Returns a rejection result if the word should not proceed, or null if OK.
        /// Sets isGive output for callers that need to set context flags.
        /// </summary>
        public static WordSubmitResult? PreprocessGive(ref string word, IGiveValidator giveValidator, out bool isGive)
        {
            isGive = TryStripGivePrefix(ref word);
            if (isGive && word.Length == 0)
                return WordSubmitResult.InvalidWord;

            if (isGive && giveValidator != null && giveValidator.RequiresItemForGive(word))
            {
                if (!giveValidator.TryConsumeForGive(word))
                    return WordSubmitResult.NoItemToGive;
            }

            return null;
        }
    }
}
