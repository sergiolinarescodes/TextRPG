using System;
using System.Collections.Generic;
using SQLite;
using TextRPG.Core.ActionExecution;
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

            [Column("target")]
            public string Target { get; set; }

            [Column("range")]
            public int? Range { get; set; }

            [Column("area")]
            public string Area { get; set; }

            [Column("assoc_word")]
            public string AssocWord { get; set; }
        }

        [Table("word_tags")]
        private class WordTagRow
        {
            [Column("word")]
            public string Word { get; set; }

            [Column("tag")]
            public string Tag { get; set; }
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

            [Column("range")]
            public int Range { get; set; }

            [Column("area")]
            public string Area { get; set; }
        }

        public static WordActionData Load(string dbPath = null)
        {
            dbPath ??= System.IO.Path.Combine(Application.streamingAssetsPath, "wordactions.db");

            var mappings = new Dictionary<string, List<WordActionMapping>>();
            var meta = new Dictionary<string, WordMeta>();
            var registry = new ActionRegistry();
            var ammoWordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

            var actionRows = db.Table<WordActionRow>().ToList();
            foreach (var row in actionRows)
            {
                var word = row.Word.ToLowerInvariant();
                var actionName = row.ActionName;
                var assocWord = row.AssocWord ?? "";

                if (!registry.Has(actionName))
                    registry.Register(actionName, new ActionDefinition(actionName, actionName, Color.gray));

                if (!mappings.TryGetValue(word, out var list))
                {
                    list = new List<WordActionMapping>();
                    mappings[word] = list;
                }

                AreaShape? actionArea = null;
                if (!string.IsNullOrEmpty(row.Area) && Enum.TryParse<AreaShape>(row.Area, out var parsedArea))
                    actionArea = parsedArea;

                list.Add(new WordActionMapping(actionName, row.Value, row.Target, row.Range, actionArea, assocWord));

                // Collect ammo words from Weapon action rows
                if (string.Equals(actionName, "Weapon", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(assocWord))
                {
                    ammoWordSet.Add(assocWord.ToLowerInvariant());
                }
            }

            var metaRows = db.Table<WordMetaRow>().ToList();
            foreach (var row in metaRows)
            {
                var word = row.Word.ToLowerInvariant();
                var area = AreaShape.Single;
                if (!string.IsNullOrEmpty(row.Area))
                    Enum.TryParse(row.Area, out area);
                meta[word] = new WordMeta(row.Target, row.Cost, row.Range, area);
            }

            var wordTags = new Dictionary<string, List<string>>();
            try
            {
                var tagRows = db.Table<WordTagRow>().ToList();
                foreach (var row in tagRows)
                {
                    var word = row.Word.ToLowerInvariant();
                    if (!wordTags.TryGetValue(word, out var tagList))
                    {
                        tagList = new List<string>();
                        wordTags[word] = tagList;
                    }
                    tagList.Add(TagNormalizer.Normalize(row.Tag));
                }
            }
            catch (SQLiteException)
            {
                // word_tags table doesn't exist yet — empty tags is fine
            }

            // Build ammo resolver from the same mappings/meta for ammo words only
            var ammoMappings = new Dictionary<string, List<WordActionMapping>>();
            var ammoMeta = new Dictionary<string, WordMeta>();

            foreach (var ammoWord in ammoWordSet)
            {
                if (mappings.TryGetValue(ammoWord, out var ammoActions))
                    ammoMappings[ammoWord] = ammoActions;
                if (meta.TryGetValue(ammoWord, out var ammoStats))
                    ammoMeta[ammoWord] = ammoStats;
            }

            var ammoResolver = new WordResolver(ammoMappings, ammoMeta);

            var tagResolver = new WordTagResolver(wordTags);
            var resolver = new WordResolver(mappings, meta);
            return new WordActionData(resolver, registry, tagResolver, ammoWordSet, ammoResolver);
        }
    }
}
