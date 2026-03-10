namespace TextRPG.Core.Run
{
    public readonly record struct RunStartedEvent(string RunId, int TotalNodes);
    public readonly record struct RunNodeStartedEvent(int NodeIndex, RunNodeType NodeType, string EncounterId);
    public readonly record struct RunNodeCompletedEvent(int NodeIndex, RunNodeType NodeType, bool Victory);
    public readonly record struct RunCompletedEvent(string RunId, bool Victory);
    public readonly record struct EscapeAttemptedEvent(bool Success);
}
