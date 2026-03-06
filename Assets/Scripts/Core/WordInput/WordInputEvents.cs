namespace TextRPG.Core.WordInput
{
    public readonly record struct WordCharacterAddedEvent(char Character, string CurrentWord);
    public readonly record struct WordSubmittedEvent(string Word);
    public readonly record struct WordClearedEvent();
}
