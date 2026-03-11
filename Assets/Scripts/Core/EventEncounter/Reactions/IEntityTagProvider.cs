using TextRPG.Core.EntityStats;

namespace TextRPG.Core.EventEncounter.Reactions
{
    public interface IEntityTagProvider
    {
        string[] GetEntityTags(EntityId entityId);
    }
}
