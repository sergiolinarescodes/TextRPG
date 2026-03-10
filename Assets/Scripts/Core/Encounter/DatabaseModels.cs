using SQLite;

namespace TextRPG.Core.Encounter
{
    internal static class DatabaseModels
    {
        [Table("units")]
        internal class UnitRow
        {
            [Column("unit_id")] public string UnitId { get; set; }
            [Column("display_name")] public string DisplayName { get; set; }
            [Column("unit_type")] public string UnitType { get; set; }
            [Column("max_health")] public int MaxHealth { get; set; }
            [Column("strength")] public int Strength { get; set; }
            [Column("magic_power")] public int MagicPower { get; set; }
            [Column("phys_defense")] public int PhysDefense { get; set; }
            [Column("magic_defense")] public int MagicDefense { get; set; }
            [Column("luck")] public int Luck { get; set; }
            [Column("starting_shield")] public int StartingShield { get; set; }
            [Column("color_r")] public float ColorR { get; set; }
            [Column("color_g")] public float ColorG { get; set; }
            [Column("color_b")] public float ColorB { get; set; }
            [Column("tier")] public int Tier { get; set; }
            [Column("dexterity")] public int Dexterity { get; set; }
            [Column("constitution")] public int Constitution { get; set; }
        }

        [Table("unit_abilities")]
        internal class UnitAbilityRow
        {
            [Column("unit_id")] public string UnitId { get; set; }
            [Column("word")] public string Word { get; set; }
        }
    }
}
