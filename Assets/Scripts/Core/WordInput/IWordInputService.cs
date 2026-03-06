namespace TextRPG.Core.WordInput
{
    public interface IWordInputService
    {
        string CurrentWord { get; }
        void AppendCharacter(char c);
        void RemoveLastCharacter();
        void SubmitWord();
        void Clear();
    }
}
