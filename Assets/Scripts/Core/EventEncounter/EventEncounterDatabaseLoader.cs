using System.Collections.Generic;
using System.IO;
using TextRPG.Core.EventEncounter.Encounters;
using SQLite;
using UnityEngine;

namespace TextRPG.Core.EventEncounter
{
    internal static class EventEncounterDatabaseLoader
    {
        [Table("event_encounters")]
        private class EncounterRow
        {
            [Column("encounter_id")] public string EncounterId { get; set; }
            [Column("display_name")] public string DisplayName { get; set; }
        }

        [Table("interactables")]
        private class InteractableRow
        {
            [Column("interactable_id")] public string InteractableId { get; set; }
            [Column("encounter_id")] public string EncounterId { get; set; }
            [Column("display_name")] public string DisplayName { get; set; }
            [Column("max_health")] public int MaxHealth { get; set; }
            [Column("color_r")] public float ColorR { get; set; }
            [Column("color_g")] public float ColorG { get; set; }
            [Column("color_b")] public float ColorB { get; set; }
            [Column("description")] public string Description { get; set; }
            [Column("slot_index")] public int SlotIndex { get; set; }
        }

        [Table("interactable_reactions")]
        private class ReactionRow
        {
            [Column("encounter_id")] public string EncounterId { get; set; }
            [Column("interactable_id")] public string InteractableId { get; set; }
            [Column("action_id")] public string ActionId { get; set; }
            [Column("outcome_id")] public string OutcomeId { get; set; }
            [Column("outcome_param")] public string OutcomeParam { get; set; }
            [Column("value")] public int Value { get; set; }
            [Column("chance")] public float Chance { get; set; }
        }

        public static void LoadIntoRegistry(EventEncounterProviderRegistry registry, string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");

            if (!File.Exists(dbPath)) return;

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

            List<EncounterRow> encounters;
            List<InteractableRow> interactableRows;
            List<ReactionRow> reactionRows;
            try
            {
                encounters = db.Table<EncounterRow>().ToList();
                interactableRows = db.Table<InteractableRow>().ToList();
                reactionRows = db.Table<ReactionRow>().ToList();
            }
            catch (SQLiteException) { return; }

            var interactablesByEncounter = new Dictionary<string, List<InteractableRow>>();
            foreach (var row in interactableRows)
            {
                if (!interactablesByEncounter.TryGetValue(row.EncounterId, out var list))
                {
                    list = new List<InteractableRow>();
                    interactablesByEncounter[row.EncounterId] = list;
                }
                list.Add(row);
            }

            var reactionsByKey = new Dictionary<string, List<InteractionReaction>>();
            foreach (var row in reactionRows)
            {
                var key = $"{row.EncounterId}:{row.InteractableId}";
                if (!reactionsByKey.TryGetValue(key, out var list))
                {
                    list = new List<InteractionReaction>();
                    reactionsByKey[key] = list;
                }
                list.Add(new InteractionReaction(row.ActionId, row.OutcomeId, row.OutcomeParam, row.Value, row.Chance));
            }

            foreach (var enc in encounters)
            {
                if (registry.Has(enc.EncounterId)) continue;

                if (!interactablesByEncounter.TryGetValue(enc.EncounterId, out var rows))
                    continue;

                rows.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

                var defs = new InteractableDefinition[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var key = $"{enc.EncounterId}:{row.InteractableId}";
                    var reactions = reactionsByKey.TryGetValue(key, out var rList)
                        ? rList.ToArray()
                        : System.Array.Empty<InteractionReaction>();

                    defs[i] = new InteractableDefinition(
                        row.DisplayName, row.MaxHealth,
                        new Color(row.ColorR, row.ColorG, row.ColorB),
                        reactions, row.Description);
                }

                var definition = new EventEncounterDefinition(enc.EncounterId, enc.DisplayName, defs);
                registry.Register(enc.EncounterId, new DatabaseEncounterProvider(definition));
            }
        }
    }
}
