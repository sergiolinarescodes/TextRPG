using System.Collections.Generic;
using System.IO;
using SQLite;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.Encounter
{
    internal static class UnitDatabaseLoader
    {
        [Table("units")]
        private class UnitRow
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
        }

        [Table("unit_abilities")]
        private class UnitAbilityRow
        {
            [Column("unit_id")] public string UnitId { get; set; }
            [Column("word")] public string Word { get; set; }
        }

        [Table("word_actions")]
        private class WordActionRow
        {
            [Column("word")] public string Word { get; set; }
            [Column("action_name")] public string ActionName { get; set; }
            [Column("value")] public int Value { get; set; }
            [Column("target")] public string Target { get; set; }
            [Column("range")] public int? Range { get; set; }
            [Column("area")] public string Area { get; set; }
        }

        [Table("unit_passives")]
        private class UnitPassiveRow
        {
            [Column("unit_id")] public string UnitId { get; set; }
            [Column("passive_id")] public string PassiveId { get; set; }
            [Column("value")] public int Value { get; set; }
        }

        [Table("word_meta")]
        private class WordMetaRow
        {
            [Column("word")] public string Word { get; set; }
            [Column("target")] public string Target { get; set; }
            [Column("cost")] public int Cost { get; set; }
            [Column("range")] public int Range { get; set; }
            [Column("area")] public string Area { get; set; }
        }

        public static Dictionary<string, EnemyDefinition> LoadAll(string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");
            var result = new Dictionary<string, EnemyDefinition>();

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            var units = db.Table<UnitRow>().ToList();
            var abilityRows = db.Table<UnitAbilityRow>().ToList();

            var abilities = new Dictionary<string, List<string>>();
            foreach (var row in abilityRows)
            {
                if (!abilities.TryGetValue(row.UnitId, out var list))
                {
                    list = new List<string>();
                    abilities[row.UnitId] = list;
                }
                list.Add(row.Word.ToUpperInvariant());
            }

            // Load passives (table may not exist in older DBs)
            var passives = new Dictionary<string, List<PassiveEntry>>();
            try
            {
                var passiveRows = db.Table<UnitPassiveRow>().ToList();
                foreach (var row in passiveRows)
                {
                    if (!passives.TryGetValue(row.UnitId, out var list))
                    {
                        list = new List<PassiveEntry>();
                        passives[row.UnitId] = list;
                    }
                    list.Add(new PassiveEntry(row.PassiveId, row.Value));
                }
            }
            catch (SQLiteException) { /* table doesn't exist yet */ }

            foreach (var unit in units)
            {
                var abilityArray = abilities.TryGetValue(unit.UnitId, out var words)
                    ? words.ToArray()
                    : System.Array.Empty<string>();

                var passiveArray = passives.TryGetValue(unit.UnitId, out var pList)
                    ? pList.ToArray()
                    : null;

                result[unit.UnitId] = new EnemyDefinition(
                    unit.DisplayName, unit.MaxHealth, unit.Strength, unit.MagicPower,
                    unit.PhysDefense, unit.MagicDefense, unit.Luck,
                    new Color(unit.ColorR, unit.ColorG, unit.ColorB), abilityArray,
                    unit.StartingShield, unit.UnitType, passiveArray);
            }

            return result;
        }

        public static void RegisterUnitWords(
            EnemyWordResolver resolver, string unitId,
            string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            var abilityRows = db.Query<UnitAbilityRow>(
                "SELECT * FROM unit_abilities WHERE unit_id = ?", unitId);

            foreach (var ability in abilityRows)
            {
                var word = ability.Word.ToLowerInvariant();

                var actionRows = db.Query<WordActionRow>(
                    "SELECT * FROM word_actions WHERE word = ?", word);
                var metaRow = db.FindWithQuery<WordMetaRow>(
                    "SELECT * FROM word_meta WHERE word = ?", word);

                var actions = new List<WordActionMapping>();
                foreach (var a in actionRows)
                {
                    ActionExecution.AreaShape? area = null;
                    if (a.Area != null && System.Enum.TryParse<ActionExecution.AreaShape>(a.Area, out var parsed))
                        area = parsed;
                    actions.Add(new WordActionMapping(a.ActionName, a.Value, a.Target, a.Range, area));
                }

                var meta = metaRow != null
                    ? new WordMeta(metaRow.Target, metaRow.Cost, metaRow.Range,
                        System.Enum.TryParse<ActionExecution.AreaShape>(metaRow.Area, out var metaArea)
                            ? metaArea : ActionExecution.AreaShape.Single)
                    : new WordMeta("SingleEnemy", 0);

                resolver.RegisterWord(word, actions, meta);
            }
        }
    }
}
