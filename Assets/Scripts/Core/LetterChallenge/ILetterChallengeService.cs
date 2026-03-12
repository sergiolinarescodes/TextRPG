using TextRPG.Core.EntityStats;

namespace TextRPG.Core.LetterChallenge
{
    /// <summary>
    /// Manages letter challenges for entities. Handles letter selection, storage,
    /// and word matching. Publishes events for UI visualization.
    ///
    /// Mode DSL (stored in passive trigger_param):
    ///   [selection]              — defaults to "contains" match
    ///   [selection]:[match_type]
    ///   [selection]:[match_type]:[match_param]
    ///
    /// Selection modes:
    ///   "vowel"       — random vowel (a,e,i,o,u)
    ///   "consonant"   — random consonant
    ///   "any"         — random letter (a-z)
    ///   "fixed:X"     — always letter X
    ///   "multi:XYZ"   — multiple letters active at once
    ///
    /// Match types:
    ///   "contains"    — word contains any active letter (default)
    ///   "starts_with" — word starts with active letter
    ///   "ends_with"   — word ends with active letter
    ///   "position:N"  — letter at position N matches
    ///   "all"         — word must contain ALL active letters
    /// </summary>
    public interface ILetterChallengeService
    {
        /// <summary>
        /// Select new letters for an entity based on mode string.
        /// Stores them and publishes <see cref="LetterChallengeStartedEvent"/>.
        /// Returns the selected letters.
        /// </summary>
        string SelectLetters(EntityId owner, string mode);

        /// <summary>
        /// Get the currently active letters for an entity, or null if none.
        /// </summary>
        string GetActiveLetters(EntityId owner);

        /// <summary>
        /// Check if a word matches the entity's active challenge.
        /// If matched, publishes <see cref="LetterChallengeMatchedEvent"/>.
        /// </summary>
        bool CheckWord(EntityId owner, string word);

        /// <summary>
        /// Clear challenge for an entity. Publishes <see cref="LetterChallengeClearedEvent"/>.
        /// </summary>
        void Clear(EntityId owner);
    }
}
