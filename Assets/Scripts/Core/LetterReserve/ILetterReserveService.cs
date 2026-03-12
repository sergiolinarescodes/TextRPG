using System.Collections.Generic;

namespace TextRPG.Core.LetterReserve
{
    public interface ILetterReserveService
    {
        /// <summary>
        /// Adds each letter from the word as a reserved charge, tagged with a source.
        /// </summary>
        void AddLetters(string word, string source);

        /// <summary>
        /// Consumes matching reserved letters for the given word and returns the count consumed.
        /// Each reserved letter can only be consumed once.
        /// </summary>
        int ConsumeMatching(string word);

        /// <summary>
        /// Checks if a specific letter has at least one reserved charge remaining.
        /// </summary>
        bool IsLetterReserved(char c);

        /// <summary>
        /// Returns all currently reserved letters (including duplicates).
        /// </summary>
        IReadOnlyList<char> GetReservedLetters();

        /// <summary>
        /// Resets all reserved state (called between encounters).
        /// </summary>
        void Clear();
    }
}
