using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.Encounter;
using TextRPG.Core.EventEncounter;
using UnityEngine;

namespace TextRPG.Core.Run
{
    internal static class RunMapGenerator
    {
        public static RunDefinition Generate(
            int nodeCount,
            Dictionary<string, EntityDefinition> allUnits,
            Dictionary<string, EventEncounterDefinition> allEventEncounters)
        {
            var nodes = new RunNode[nodeCount];
            var enemies = allUnits.Where(kvp => kvp.Value.UnitType == "enemy").ToList();
            var tier1 = enemies.Where(e => e.Value.Tier == 1).ToList();
            var tier2 = enemies.Where(e => e.Value.Tier == 2).ToList();
            if (tier2.Count == 0) tier2 = tier1;
            var events = allEventEncounters.Values.ToList();

            // Final node is always a Boss
            for (int i = 0; i < nodeCount - 1; i++)
            {
                var roll = UnityEngine.Random.value;
                RunNodeType nodeType;
                if (roll < 0.55f)
                    nodeType = RunNodeType.Combat;
                else if (roll < 0.60f)
                    nodeType = RunNodeType.EliteCombat;
                else
                    nodeType = RunNodeType.Event;

                var pool = PickPool(i, nodeCount, tier1, tier2);
                nodes[i] = CreateNode(i, nodeType, pool, events);
            }

            // Boss node — always tier 2
            nodes[nodeCount - 1] = CreateBossNode(nodeCount - 1, tier2);

            var runId = $"run_{DateTime.UtcNow.Ticks}";
            return new RunDefinition(runId, nodes);
        }

        private static List<KeyValuePair<string, EntityDefinition>> PickPool(
            int index, int totalNodes,
            List<KeyValuePair<string, EntityDefinition>> tier1,
            List<KeyValuePair<string, EntityDefinition>> tier2)
        {
            float progress = (float)index / totalNodes;
            if (progress < 0.4f)
                return tier1;
            if (progress > 0.7f)
                return tier2;
            var mixed = new List<KeyValuePair<string, EntityDefinition>>(tier1);
            mixed.AddRange(tier2);
            return mixed;
        }

        private static RunNode CreateNode(
            int index,
            RunNodeType nodeType,
            List<KeyValuePair<string, EntityDefinition>> enemies,
            List<EventEncounterDefinition> events)
        {
            switch (nodeType)
            {
                case RunNodeType.Combat:
                    return new RunNode(index, RunNodeType.Combat,
                        GenerateCombatEncounter(index, enemies, 1f), null);

                case RunNodeType.EliteCombat:
                    return new RunNode(index, RunNodeType.EliteCombat,
                        GenerateCombatEncounter(index, enemies, 1.5f), null);

                case RunNodeType.Event:
                    if (events.Count == 0)
                        return new RunNode(index, RunNodeType.Combat,
                            GenerateCombatEncounter(index, enemies, 1f), null);

                    var evt = events[UnityEngine.Random.Range(0, events.Count)];
                    return new RunNode(index, RunNodeType.Event, null, evt);

                default:
                    return new RunNode(index, RunNodeType.Combat,
                        GenerateCombatEncounter(index, enemies, 1f), null);
            }
        }

        private static RunNode CreateBossNode(int index, List<KeyValuePair<string, EntityDefinition>> enemies)
        {
            var encounter = GenerateCombatEncounter(index, enemies, 2f);
            return new RunNode(index, RunNodeType.Boss,
                new EncounterDefinition($"boss_{index}", "BOSS FIGHT", encounter.Enemies), null);
        }

        private static EncounterDefinition GenerateCombatEncounter(
            int index,
            List<KeyValuePair<string, EntityDefinition>> enemies,
            float statMultiplier)
        {
            if (enemies.Count == 0)
                return new EncounterDefinition($"encounter_{index}", "Empty", Array.Empty<EntityDefinition>());

            int count = UnityEngine.Random.Range(2, 4); // 2-3 enemies
            var picked = new EntityDefinition[count];
            for (int i = 0; i < count; i++)
            {
                var kvp = enemies[UnityEngine.Random.Range(0, enemies.Count)];
                var def = kvp.Value;

                if (Math.Abs(statMultiplier - 1f) > 0.01f)
                {
                    picked[i] = new EntityDefinition(
                        def.Name,
                        Mathf.RoundToInt(def.MaxHealth * statMultiplier),
                        Mathf.RoundToInt(def.Strength * statMultiplier),
                        Mathf.RoundToInt(def.MagicPower * statMultiplier),
                        Mathf.RoundToInt(def.PhysicalDefense * statMultiplier),
                        Mathf.RoundToInt(def.MagicDefense * statMultiplier),
                        def.Luck,
                        def.Color,
                        def.Abilities,
                        Mathf.RoundToInt(def.StartingShield * statMultiplier),
                        def.UnitType,
                        def.Passives,
                        def.Tags,
                        def.Description,
                        def.DeathReward,
                        def.DeathRewardValue,
                        def.Tier,
                        Mathf.RoundToInt(def.Dexterity * statMultiplier),
                        Mathf.RoundToInt(def.Constitution * statMultiplier)
                    );
                }
                else
                {
                    picked[i] = def;
                }
            }

            var id = $"encounter_{index}";
            var displayName = statMultiplier > 1.5f ? "Boss Encounter" :
                statMultiplier > 1f ? "Elite Encounter" : "Combat";
            return new EncounterDefinition(id, displayName, picked);
        }
    }
}
