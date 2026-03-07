using UnityEngine;

namespace TextRPG.Core.UnitRendering
{
    public readonly record struct UnitDefinition(
        UnitId Id, string Name, int MaxHp,
        int Strength, int Dexterity, int Intelligence, Color Color);
}
