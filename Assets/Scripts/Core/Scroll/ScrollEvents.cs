namespace TextRPG.Core.Scroll
{
    public readonly record struct SpellLearnedEvent(string ScrambledWord, string OriginalWord, int ManaCost);
    public readonly record struct ScrollAcquiredEvent(string ItemKey, ScrollDefinition Scroll);
}
