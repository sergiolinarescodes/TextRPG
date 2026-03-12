using TextRPG.Core.EntityStats;

namespace TextRPG.Core.LetterChallenge
{
    public readonly record struct LetterChallengeStartedEvent(EntityId Owner, string Letters, string Mode);
    public readonly record struct LetterChallengeMatchedEvent(EntityId Owner, string Letters, string Word);
    public readonly record struct LetterChallengeClearedEvent(EntityId Owner);
}
