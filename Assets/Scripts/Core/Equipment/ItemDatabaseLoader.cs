using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using TextRPG.Core.Encounter;
using TextRPG.Core.WordAction;
using UnityEngine;

namespace TextRPG.Core.Equipment
{
    internal static class ItemDatabaseLoader
    {
        [Table("items")]
        private class ItemRow
        {
            [Column("item_id")] public string ItemId { get; set; }
            [Column("display_name")] public string DisplayName { get; set; }
            [Column("item_type")] public string ItemType { get; set; }
            [Column("durability")] public int Durability { get; set; }
            [Column("strength")] public int Strength { get; set; }
            [Column("magic_power")] public int MagicPower { get; set; }
            [Column("phys_defense")] public int PhysDefense { get; set; }
            [Column("magic_defense")] public int MagicDefense { get; set; }
            [Column("luck")] public int Luck { get; set; }
            [Column("max_health")] public int MaxHealth { get; set; }
            [Column("max_mana")] public int MaxMana { get; set; }
            [Column("color_r")] public float ColorR { get; set; }
            [Column("color_g")] public float ColorG { get; set; }
            [Column("color_b")] public float ColorB { get; set; }
        }

        [Table("item_passives")]
        private class ItemPassiveRow
        {
            [Column("item_id")] public string ItemId { get; set; }
            [Column("trigger_id")] public string TriggerId { get; set; }
            [Column("trigger_param")] public string TriggerParam { get; set; }
            [Column("effect_id")] public string EffectId { get; set; }
            [Column("effect_param")] public string EffectParam { get; set; }
            [Column("value")] public int Value { get; set; }
            [Column("target")] public string Target { get; set; }
        }

        public static Dictionary<string, EquipmentItemDefinition> LoadAll(
            WordActionData wordActionData = null, string dbPath = null)
        {
            dbPath ??= Path.Combine(Application.streamingAssetsPath, "wordactions.db");
            var result = new Dictionary<string, EquipmentItemDefinition>(StringComparer.OrdinalIgnoreCase);

            using var db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

            List<ItemRow> items;
            try
            {
                items = db.Table<ItemRow>().ToList();
            }
            catch (SQLiteException)
            {
                return result;
            }

            var passives = new Dictionary<string, List<PassiveEntry>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var passiveRows = db.Table<ItemPassiveRow>().ToList();
                foreach (var row in passiveRows)
                {
                    if (!passives.TryGetValue(row.ItemId, out var list))
                    {
                        list = new List<PassiveEntry>();
                        passives[row.ItemId] = list;
                    }
                    list.Add(new PassiveEntry(
                        row.TriggerId, row.TriggerParam,
                        row.EffectId, row.EffectParam,
                        row.Value, row.Target ?? "Self"));
                }
            }
            catch (SQLiteException) { }

            // Use pre-built ammo-per-item lookup from WordActionData
            var ammoWordsByItem = wordActionData?.AmmoWordsByItem;

            foreach (var item in items)
            {
                var slotType = ParseSlotType(item.ItemType);
                var stats = new StatBonus(
                    item.Strength, item.MagicPower, item.PhysDefense, item.MagicDefense,
                    item.Luck, item.MaxHealth, item.MaxMana);
                var color = new Color(item.ColorR, item.ColorG, item.ColorB);
                string[] ammoWords;
                if (ammoWordsByItem != null && ammoWordsByItem.TryGetValue(item.ItemId, out var ammo))
                {
                    ammoWords = new string[ammo.Count];
                    for (int i = 0; i < ammo.Count; i++) ammoWords[i] = ammo[i];
                }
                else
                {
                    ammoWords = Array.Empty<string>();
                }
                var passiveArray = passives.TryGetValue(item.ItemId, out var pList)
                    ? pList.ToArray()
                    : Array.Empty<PassiveEntry>();

                result[item.ItemId] = new EquipmentItemDefinition(
                    item.ItemId, item.DisplayName, slotType, item.Durability,
                    stats, color, ammoWords, passiveArray);
            }

            return result;
        }

        private static EquipmentSlotType ParseSlotType(string itemType)
        {
            var result = itemType?.ToLowerInvariant() switch
            {
                "weapon" => EquipmentSlotType.Weapon,
                "head" => EquipmentSlotType.Head,
                "wear" => EquipmentSlotType.Wear,
                "trinket" => EquipmentSlotType.Trinket,
                "accessory" => EquipmentSlotType.Accessory,
                _ => EquipmentSlotType.Accessory,
            };
            if (result == EquipmentSlotType.Accessory && itemType != null
                && !string.Equals(itemType, "accessory", StringComparison.OrdinalIgnoreCase))
                Debug.LogWarning($"[ItemDatabaseLoader] Unknown item_type '{itemType}', defaulting to Accessory");
            return result;
        }
    }
}
