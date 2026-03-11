namespace TextRPG.Core.EventEncounter.Reactions.Tags
{
    public interface ITagDefinition
    {
        string TagId { get; }
        void React(TagReactionContext context);
    }
}
