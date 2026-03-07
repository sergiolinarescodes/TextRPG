using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering.Scenarios
{
    internal sealed class UnitRenderingScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter GridWidthParam = new(
            "gridWidth", "Grid Width", typeof(int), 5, 2, 10);

        private static readonly ScenarioParameter GridHeightParam = new(
            "gridHeight", "Grid Height", typeof(int), 5, 2, 10);

        private static readonly ScenarioParameter UnitNamesParam = new(
            "unitNames", "Unit Names (comma-separated)", typeof(string), "HUNTER,ORC,GOBLIN,AI,DRAGONKNIGHT");

        private IEventBus _eventBus;
        private IUnitService _unitService;
        private IGrid<UnitId?> _grid;
        private int _gridWidth;
        private int _gridHeight;
        private readonly List<IDisposable> _subscriptions = new();
        private UnitGridVisual _gridVisual;
        private VisualElement _tileMapPanel;

        public UnitRenderingScenario() : base(new TestScenarioDefinition(
            "unit-rendering",
            "Unit Rendering (Grid)",
            "Displays units on a grid with text wrapping using most-square-fit layout. " +
            "Unit names are split into rows and font-sized to fill each tile.",
            new[] { GridWidthParam, GridHeightParam, UnitNamesParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _gridWidth = Mathf.Clamp(ResolveParam<int>(overrides, "gridWidth"), 2, 10);
            _gridHeight = Mathf.Clamp(ResolveParam<int>(overrides, "gridHeight"), 2, 10);
            var unitNamesRaw = ResolveParam<string>(overrides, "unitNames");
            var unitNames = unitNamesRaw.Split(',');

            _eventBus = new EventBus();
            _unitService = new UnitService(_eventBus);
            var gridFactory = new GridFactory(_eventBus);
            _grid = gridFactory.Create<UnitId?>(_gridWidth, _gridHeight, 1f);

            _subscriptions.Add(_eventBus.Subscribe<UnitPlacedEvent>(evt =>
            {
                Debug.Log($"[UnitRenderingScenario] Placed unit {evt.UnitId.Value} at {evt.Position}");
            }));

            _subscriptions.Add(_eventBus.Subscribe<GridCellChangedEvent>(evt =>
            {
                _gridVisual?.UpdateCell(evt.Position);
            }));

            // Place units in a line from bottom-left
            var colors = new[] { Color.green, Color.red, Color.cyan, Color.yellow, Color.magenta };
            for (int i = 0; i < unitNames.Length; i++)
            {
                var name = unitNames[i].Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(name)) continue;

                int x = i % _gridWidth;
                int y = i / _gridWidth;
                if (y >= _gridHeight) break;

                var pos = new GridPosition(x, y);
                var def = new UnitDefinition(
                    new UnitId(name.ToLowerInvariant()),
                    name, 100, 10, 8, 12,
                    colors[i % colors.Length]);
                _unitService.PlaceUnit(def, pos, _grid);
            }

            BuildUI();

            Debug.Log($"[UnitRenderingScenario] Started — grid={_gridWidth}x{_gridHeight}, units={unitNames.Length}");
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = Color.black;

            _tileMapPanel = new VisualElement();
            _tileMapPanel.style.flexGrow = 1;
            _tileMapPanel.style.backgroundColor = Color.black;
            _tileMapPanel.style.flexDirection = FlexDirection.ColumnReverse;
            _tileMapPanel.style.paddingTop = 4;
            _tileMapPanel.style.paddingBottom = 4;
            _tileMapPanel.style.paddingLeft = 4;
            _tileMapPanel.style.paddingRight = 4;
            root.Add(_tileMapPanel);

            _gridVisual = new UnitGridVisual(_grid, _unitService, null, _gridWidth, _gridHeight);
            _tileMapPanel.RegisterCallback<GeometryChangedEvent>(_ => _gridVisual?.RefreshFontSizes());
            _gridVisual.Build(_tileMapPanel);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var expectedCellCount = _gridWidth * _gridHeight;
            var actualCellCount = _gridVisual?.Cells.Count ?? 0;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new($"Grid cells spawned ({expectedCellCount})",
                    actualCellCount == expectedCellCount,
                    actualCellCount == expectedCellCount
                        ? null : $"Expected {expectedCellCount}, got {actualCellCount}"),
                new("Unit service created", _unitService != null,
                    _unitService != null ? null : "Unit service is null"),
                new("Grid created", _grid != null,
                    _grid != null ? null : "Grid is null"),
                new("Tile map panel exists", _tileMapPanel != null,
                    _tileMapPanel != null ? null : "Tile map panel is null")
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
            _unitService = null;
            _grid = null;
            _gridVisual = null;
            _tileMapPanel = null;
        }
    }
}
