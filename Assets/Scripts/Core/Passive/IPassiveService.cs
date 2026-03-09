using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public interface IPassiveService
    {
        void RegisterPassives(EntityId entityId, PassiveEntry[] passives);
        void RegisterPassives(EntityId entityId, string source, PassiveEntry[] passives);
        void RemovePassives(EntityId entityId);
        void RemovePassives(EntityId entityId, string source);
        bool HasPassives(EntityId entityId);
        IReadOnlyList<PassiveEntry> GetPassives(EntityId entityId);
        bool HasTaunt(EntityId entityId);
    }
}
