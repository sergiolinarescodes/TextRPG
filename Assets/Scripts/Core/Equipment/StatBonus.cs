namespace TextRPG.Core.Equipment
{
    public readonly record struct StatBonus(
        int Strength, int MagicPower, int PhysDefense, int MagicDefense,
        int Luck, int MaxHealth, int MaxMana);
}
