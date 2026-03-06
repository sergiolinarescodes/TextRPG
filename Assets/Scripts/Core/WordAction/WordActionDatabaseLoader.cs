using System.Collections.Generic;
using SQLite;
using UnityEngine;

namespace TextRPG.Core.WordAction
{
    internal static class WordActionDatabaseLoader
    {
        [Table("word_actions")]
        private class WordActionRow
        {
            [Column("word")]
            public string Word { get; set; }

            [Column("action_name")]
            public string ActionName { get; set; }

            [Column("value")]
            public int Value { get; set; }
        }

        [Table("word_meta")]
        private class WordMetaRow
        {
            [Column("word")]
            public string Word { get; set; }

            [Column("target")]
            public string Target { get; set; }

            [Column("cost")]
            public int Cost { get; set; }
        }

        public static WordActionData Load(string dbPath = null)
        {
            dbPath ??= System.IO.Path.Combine(Application.streamingAssetsPath, "wordactions.db");

            var mappings = new Dictionary<string, List<WordActionMapping>>();
            var meta = new Dictionary<string, WordMeta>();
            var registry = new ActionRegistry();

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

            var actionRows = db.Table<WordActionRow>().ToList();
            foreach (var row in actionRows)
            {
                var word = row.Word.ToLowerInvariant();
                var actionName = row.ActionName;

                if (!registry.Has(actionName))
                    registry.Register(actionName, new ActionDefinition(actionName, actionName));

                if (!mappings.TryGetValue(word, out var list))
                {
                    list = new List<WordActionMapping>();
                    mappings[word] = list;
                }

                list.Add(new WordActionMapping(actionName, row.Value));
            }

            var metaRows = db.Table<WordMetaRow>().ToList();
            foreach (var row in metaRows)
            {
                var word = row.Word.ToLowerInvariant();
                meta[word] = new WordMeta(row.Target, row.Cost);
            }

            var resolver = new WordResolver(mappings, meta);
            return new WordActionData(resolver, registry);
        }
    }
}
