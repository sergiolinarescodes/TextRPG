using System.Collections.Generic;
using System.IO;
using SQLite;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.Encounter
{
    internal static class UnitDatabaseLoader
    {
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
            [Column("trigger_id")] public string TriggerId { get; set; }
            [Column("trigger_param")] public string TriggerParam { get; set; }
            [Column("effect_id")] public string EffectId { get; set; }
            [Column("effect_param")] public string EffectParam { get; set; }
            [Column("value")] public int Value { get; set; }
            [Column("target")] public string Target { get; set; }
        }

        [Table("unit_tags")]
        private class UnitTagRow
        {
            [Column("unit_id")] public string UnitId { get; set; }
            [Column("tag")] public string Tag { get; set; }
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

        public static Dictionary<string, EntityDefinition> LoadAll(string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");
            var result = new Dictionary<string, EntityDefinition>();

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            var units = db.Table<DatabaseModels.UnitRow>().ToList();
            var abilityRows = db.Table<DatabaseModels.UnitAbilityRow>().ToList();

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
                    list.Add(new PassiveEntry(row.TriggerId, row.TriggerParam, row.EffectId, row.EffectParam, row.Value, row.Target ?? "Self"));
                }
            }
            catch (SQLiteException) { /* table doesn't exist yet */ }

            // Load tags (table may not exist in older DBs)
            var tags = new Dictionary<string, List<string>>();
            try
            {
                var tagRows = db.Table<UnitTagRow>().ToList();
                foreach (var row in tagRows)
                {
                    if (!tags.TryGetValue(row.UnitId, out var list))
                    {
                        list = new List<string>();
                        tags[row.UnitId] = list;
                    }
                    list.Add(row.Tag);
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

                var tagArray = tags.TryGetValue(unit.UnitId, out var tList)
                    ? tList.ToArray()
                    : null;

                result[unit.UnitId] = new EntityDefinition(
                    unit.DisplayName, unit.MaxHealth, unit.Strength, unit.MagicPower,
                    unit.PhysDefense, unit.MagicDefense, unit.Luck,
                    new Color(unit.ColorR, unit.ColorG, unit.ColorB), abilityArray,
                    unit.StartingShield, unit.UnitType, passiveArray, tagArray,
                    Tier: unit.Tier, Dexterity: unit.Dexterity, Constitution: unit.Constitution);
            }

            return result;
        }

        public static void RegisterUnitWords(
            EnemyWordResolver resolver, string unitId,
            string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
            var abilityRows = db.Query<DatabaseModels.UnitAbilityRow>(
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
