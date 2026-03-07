namespace TextRPG.Core.Weapon
{
    internal sealed class WeaponStateEntry
    {
        public WeaponDefinition Definition { get; }
        public int CurrentDurability { get; set; }

        public WeaponStateEntry(WeaponDefinition definition)
        {
            Definition = definition;
            CurrentDurability = definition.Durability;
        }
    }
}
