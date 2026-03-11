namespace TextRPG.Core.WordCooldown
{
    public interface IWordCooldownService
    {
        bool CanUseWord(string word, int currentRound);
        void MarkWordUsed(string word, int currentRound);
        int GetRemainingCooldown(string word, int currentRound);
        int GetUseCount(string word);
        bool IsPermanentlyBanned(string word);
        void RegisterFixedCooldown(string word, int rounds);
        void Reset();
    }
}
