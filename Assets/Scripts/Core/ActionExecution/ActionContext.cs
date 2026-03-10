using System.Collections.Generic;
using TextRPG.Core.EntityStats;

namespace TextRPG.Core.ActionExecution
{
    public readonly record struct ActionContext(EntityId Source, IReadOnlyList<EntityId> Targets, int Value, string Word, string AssocWord = "");
}
