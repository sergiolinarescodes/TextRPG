namespace TextRPG.Core.WordCooldown
{
    public readonly record struct WordCooldownEvent(string Word, int RemainingRounds, bool Permanent);
}
