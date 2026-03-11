using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;

namespace TextRPG.Core.Effects
{
    public readonly record struct EffectContext(
        EntityId Source,
        IReadOnlyList<EntityId> Targets,
        int Value,
        string Param,
        IGameServices Services
    );
}
