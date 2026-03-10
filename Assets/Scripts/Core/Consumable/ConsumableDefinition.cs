using System.Collections.Generic;

namespace TextRPG.Core.Consumable
{
    public readonly record struct ConsumableDefinition(
        string Word,
        string DisplayName,
        int Durability,
        IReadOnlyList<string> AmmoWords);
}
