using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.Passive
{
    public interface IPassiveEffect
    {
        string EffectId { get; }
        void Execute(EntityId owner, int value, string effectParam,
                     IReadOnlyList<EntityId> targets, IPassiveContext ctx);
    }
}
