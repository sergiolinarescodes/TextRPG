using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public interface IPassiveService
    {
        void RegisterPassives(EntityId entityId, PassiveEntry[] passives);
        void RemovePassives(EntityId entityId);
        bool HasPassives(EntityId entityId);
        IReadOnlyList<PassiveEntry> GetPassives(EntityId entityId);
    }
}
