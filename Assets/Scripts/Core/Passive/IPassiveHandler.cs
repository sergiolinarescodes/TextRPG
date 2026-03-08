using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public interface IPassiveHandler
    {
        string PassiveId { get; }
        void Register(EntityId owner, int value, IPassiveContext context);
        void Unregister(EntityId owner, IPassiveContext context);
    }
}
