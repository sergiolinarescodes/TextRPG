using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.CombatGrid.Scenarios
{
    internal sealed class CombatGridScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter GridWidthParam = new(
            "gridWidth", "Grid Width", typeof(int), 6, 3, 12);
        private static readonly ScenarioParameter GridHeightParam = new(
            "gridHeight", "Grid Height", typeof(int), 4, 3, 12);

        private ICombatGridService _gridService;
        private IEventBus _eventBus;
        private EntityId _hero;
        private EntityId _enemy;
        private int _adjacentCount;
        private int _rangeCount;
        private int _distance;
        private bool _moved;
        private readonly List<IDisposable> _subscriptions = new();

        public CombatGridScenario() : base(new TestScenarioDefinition(
            "combat-grid",
            "Combat Grid",
            "Places entities on a grid, queries adjacency and range, moves an entity.",
            new[] { GridWidthParam, GridHeightParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var width = ResolveParam<int>(overrides, "gridWidth");
            var height = ResolveParam<int>(overrides, "gridHeight");

            _eventBus = new EventBus();
            var unitService = new UnitService(_eventBus);
            _gridService = new CombatGridService(_eventBus, unitService);

            _gridService.Initialize(width, height);

            _hero = new EntityId("hero");
            _enemy = new EntityId("enemy");

            var heroDef = new UnitDefinition(new UnitId("hero"), "HERO", 100, 10, 5, 8, Color.cyan);
            var enemyDef = new UnitDefinition(new UnitId("enemy"), "ORC", 50, 8, 3, 2, Color.green);

            _gridService.RegisterCombatant(_hero, heroDef, new GridPosition(1, 1));
            _gridService.RegisterCombatant(_enemy, enemyDef, new GridPosition(3, 1));

            _distance = _gridService.GetDistance(_hero, _enemy);
            _adjacentCount = _gridService.GetAdjacentEntities(new GridPosition(1, 1)).Count;
            _rangeCount = _gridService.GetEntitiesInRange(new GridPosition(1, 1), 3).Count;

            _subscriptions.Add(_eventBus.Subscribe<CombatantMovedEvent>(e =>
            {
                _moved = true;
                Debug.Log($"[CombatGridScenario] {e.EntityId.Value} moved from {e.From} to {e.To}");
            }));

            _gridService.MoveEntity(_hero, new GridPosition(2, 1));

            BuildUI(width, height);
        }

        private void BuildUI(int width, int height)
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;

            var title = new Label("Combat Grid");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Grid Size", $"{width}x{height}", new Color(0.6f, 0.8f, 1f));
            AddInfoRow(root, "Distance", _distance.ToString(), new Color(1f, 0.85f, 0.3f));
            AddInfoRow(root, "Adjacent", _adjacentCount.ToString(), new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "In Range (3)", _rangeCount.ToString(), new Color(1f, 0.6f, 0.2f));
            AddInfoRow(root, "Moved", _moved.ToString(), new Color(0.8f, 0.8f, 0.3f));

            var newDist = _gridService.GetDistance(_hero, _enemy);
            AddInfoRow(root, "New Distance", newDist.ToString(), new Color(0.6f, 1f, 0.6f));
        }

        private static void AddInfoRow(VisualElement parent, string label, string value, Color valueColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;

            var nameLabel = new Label($"{label}: ");
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.width = 150;
            row.Add(nameLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 16;
            valueLabel.style.color = valueColor;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Grid service initialized", _gridService != null && _gridService.Grid != null,
                    _gridService?.Grid != null ? null : "Grid is null"),
                new("Hero registered", _gridService.GetEntityAt(_gridService.GetPosition(_hero)) != null,
                    "Hero not found on grid"),
                new("Enemy registered", _gridService.GetEntityAt(_gridService.GetPosition(_enemy)) != null,
                    "Enemy not found on grid"),
                new("Distance correct", _distance == 2,
                    _distance == 2 ? null : $"Expected distance 2 but got {_distance}"),
                new("Move succeeded", _moved,
                    _moved ? null : "Entity did not move"),
                new("New distance is 1", _gridService.GetDistance(_hero, _enemy) == 1,
                    _gridService.GetDistance(_hero, _enemy) == 1 ? null : "Expected distance 1 after move"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_gridService as IDisposable)?.Dispose();
            _gridService = null;
            _eventBus = null;
        }
    }
}
