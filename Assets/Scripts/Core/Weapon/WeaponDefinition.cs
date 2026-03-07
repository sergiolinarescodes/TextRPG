using System.Collections.Generic;

namespace TextRPG.Core.Weapon
{
    public readonly record struct WeaponDefinition(
        string WeaponWord,
        string DisplayName,
        int Durability,
        IReadOnlyList<string> AmmoWords);
}
