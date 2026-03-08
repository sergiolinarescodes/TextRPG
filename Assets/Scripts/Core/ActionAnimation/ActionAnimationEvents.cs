namespace TextRPG.Core.ActionAnimation
{
    public readonly record struct ActionAnimationStartedEvent(string Word, int ActionCount);
    public readonly record struct ActionAnimationCompletedEvent(string Word);
}
