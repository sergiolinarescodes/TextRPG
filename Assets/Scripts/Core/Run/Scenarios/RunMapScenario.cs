using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.Encounter;
using TextRPG.Core.EventEncounter;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.Run.Scenarios
{
    internal sealed class RunMapScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter NodeCountParam = new(
            "nodeCount", "Node Count", typeof(int), 10, 3, 20);

        private RunDefinition _runDefinition;

        public RunMapScenario() : base(new TestScenarioDefinition(
            "run-map",
            "Run Map Generator",
            "Generates a run map and displays nodes as a vertical list showing type and encounter name.",
            new[] { NodeCountParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var nodeCount = ResolveParam<int>(overrides, "nodeCount");

            var allUnits = UnitDatabaseLoader.LoadAll();
            var allEventEncounters = LoadEventEncounters();

            _runDefinition = RunMapGenerator.Generate(nodeCount, allUnits, allEventEncounters);

            BuildUI();

            Debug.Log($"[RunMapScenario] Generated run with {_runDefinition.Nodes.Length} nodes");
            foreach (var node in _runDefinition.Nodes)
            {
                var name = node.CombatEncounter?.DisplayName ?? node.EventEncounter?.DisplayName ?? "?";
                Debug.Log($"  Node {node.Index}: {node.NodeType} — {name}");
            }
        }

        private static Dictionary<string, EventEncounterDefinition> LoadEventEncounters()
        {
            var registry = new EventEncounterProviderRegistry();
            EventEncounterDatabaseLoader.LoadIntoRegistry(registry);

            var result = new Dictionary<string, EventEncounterDefinition>();
            foreach (var key in registry.Keys)
            {
                if (registry.TryGet(key, out var provider))
                    result[key] = provider.CreateDefinition();
            }
            return result;
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            var title = new Label($"Run Map — {_runDefinition.Nodes.Length} Nodes");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            root.Add(scrollView);

            foreach (var node in _runDefinition.Nodes)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 8;
                row.style.paddingLeft = 8;
                row.style.paddingRight = 8;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 6;
                row.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
                row.style.borderTopLeftRadius = 4;
                row.style.borderTopRightRadius = 4;
                row.style.borderBottomLeftRadius = 4;
                row.style.borderBottomRightRadius = 4;

                var indexLabel = new Label($"{node.Index + 1}.");
                indexLabel.style.fontSize = 18;
                indexLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                indexLabel.style.width = 40;
                indexLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                indexLabel.style.marginRight = 12;
                row.Add(indexLabel);

                var typeColor = GetNodeTypeColor(node.NodeType);
                var typeLabel = new Label(node.NodeType.ToString().ToUpperInvariant());
                typeLabel.style.fontSize = 16;
                typeLabel.style.color = typeColor;
                typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                typeLabel.style.width = 120;
                row.Add(typeLabel);

                var name = node.CombatEncounter?.DisplayName ?? node.EventEncounter?.DisplayName ?? "?";
                var nameLabel = new Label(name);
                nameLabel.style.fontSize = 16;
                nameLabel.style.color = Color.white;
                nameLabel.style.flexGrow = 1;
                row.Add(nameLabel);

                if (node.CombatEncounter != null)
                {
                    var enemies = string.Join(", ", node.CombatEncounter.Enemies.Select(e => e.Name));
                    var detailLabel = new Label(enemies);
                    detailLabel.style.fontSize = 14;
                    detailLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    row.Add(detailLabel);
                }

                scrollView.Add(row);
            }
        }

        private static Color GetNodeTypeColor(RunNodeType type) => type switch
        {
            RunNodeType.Combat => new Color(0.8f, 0.3f, 0.3f),
            RunNodeType.EliteCombat => new Color(1f, 0.6f, 0.2f),
            RunNodeType.Boss => new Color(1f, 0.2f, 0.8f),
            RunNodeType.Event => new Color(0.3f, 0.8f, 0.3f),
            _ => Color.white
        };

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var nodes = _runDefinition?.Nodes;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Run definition generated", _runDefinition != null,
                    _runDefinition != null ? null : "No run definition"),
                new("Correct node count", nodes?.Length == ResolveParam<int>(overrides, "nodeCount"),
                    nodes != null ? $"Got {nodes.Length}" : "No nodes"),
                new("Last node is Boss",
                    nodes != null && nodes.Length > 0 && nodes[^1].NodeType == RunNodeType.Boss,
                    nodes != null && nodes.Length > 0
                        ? $"Last node is {nodes[^1].NodeType}"
                        : "No nodes"),
                new("Boss has combat encounter",
                    nodes != null && nodes.Length > 0 && nodes[^1].CombatEncounter != null,
                    "Boss node missing combat encounter"),
                new("All combat nodes have encounters",
                    nodes != null && nodes.All(n =>
                        n.NodeType == RunNodeType.Event || n.CombatEncounter != null),
                    "Some combat nodes missing encounters"),
                new("All event nodes have event encounters",
                    nodes != null && nodes.All(n =>
                        n.NodeType != RunNodeType.Event || n.EventEncounter != null),
                    "Some event nodes missing event encounters"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            _runDefinition = null;
        }
    }
}
