namespace TextRPG.Core.ActionExecution
{
    public interface IGiveValidator
    {
        bool RequiresItemForGive(string word);
        bool TryConsumeForGive(string word);
    }
}
