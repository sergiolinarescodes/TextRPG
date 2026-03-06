using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using Unidad.Core.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.WordInput.Scenarios
{
    internal sealed class WordInputScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter VibrationAmplitudeParam = new(
            "vibrationAmplitude", "Vibration Amplitude", typeof(float), 3.0f, 0f, 10f);

        private static readonly ScenarioParameter GridWidthParam = new(
            "gridWidth", "Grid Width", typeof(int), 3, 1, 5);

        private static readonly ScenarioParameter GridHeightParam = new(
            "gridHeight", "Grid Height", typeof(int), 12, 3, 20);

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private IEventBus _eventBus;
        private IWordInputService _service;
        private AnimatedCodeField _codeField;
        private VisualElement _mainTextPanel;
        private VisualElement _statsBar;
        private IGrid<int> _grid;
        private int _gridWidth;
        private int _gridHeight;
        private float _fontScaleFactor;
        private VisualElement _linesContainer;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<VisualElement> _gridCells = new();

        public WordInputScenario() : base(new TestScenarioDefinition(
            "word-input",
            "Word Input (Live)",
            "Full-screen word input with auto-scaling text, vibration animation, " +
            "tile map grid, and stats bar. Type a word and press Enter to submit.",
            new[] { VibrationAmplitudeParam, GridWidthParam, GridHeightParam, FontScaleFactorParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
            _gridWidth = Mathf.Clamp(ResolveParam<int>(overrides, "gridWidth"), 1, 5);
            _gridHeight = Mathf.Clamp(ResolveParam<int>(overrides, "gridHeight"), 3, 20);
            _fontScaleFactor = ResolveParam<float>(overrides, "fontScaleFactor");

            // --- Services ---
            _eventBus = new Unidad.Core.EventBus.EventBus();
            _service = new WordInputService(_eventBus);
            var gridFactory = new GridFactory(_eventBus);
            _grid = gridFactory.Create<int>(_gridWidth, _gridHeight, 1f);

            // Place player at center-bottom
            var playerPos = new GridPosition(_gridWidth / 2, 0);
            _grid.Set(playerPos, 1);

            // --- Subscribe to events ---
            _subscriptions.Add(_eventBus.Subscribe<WordSubmittedEvent>(evt =>
            {
                Debug.Log($"[WordInputScenario] Submitted: \"{evt.Word}\"");
            }));

            _subscriptions.Add(_eventBus.Subscribe<WordClearedEvent>(_ =>
            {
                Debug.Log("[WordInputScenario] Word cleared");
            }));

            _subscriptions.Add(_eventBus.Subscribe<GridCellChangedEvent>(evt =>
            {
                UpdateGridCellVisual(evt.Position);
            }));

            // --- Build UI ---
            BuildUI(vibrationAmplitude);

            Debug.Log($"[WordInputScenario] Started — grid={_gridWidth}x{_gridHeight}, vibration={vibrationAmplitude}, fontScale={_fontScaleFactor}");
        }

        private void BuildUI(float vibrationAmplitude)
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Row;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            // --- Left Column ---
            var leftColumn = new VisualElement();
            leftColumn.style.flexGrow = 4;
            leftColumn.style.flexDirection = FlexDirection.Column;
            root.Add(leftColumn);

            // Main text panel
            _mainTextPanel = new VisualElement();
            _mainTextPanel.style.flexGrow = 9;
            _mainTextPanel.style.backgroundColor = Color.black;
            _mainTextPanel.style.justifyContent = Justify.Center;
            _mainTextPanel.style.alignItems = Align.Stretch;
            _mainTextPanel.style.overflow = Overflow.Hidden;
            leftColumn.Add(_mainTextPanel);

            // AnimatedCodeField
            _codeField = new AnimatedCodeField();
            _codeField.multiline = false;
            _codeField.TypingAnimationAmplitude = vibrationAmplitude;
            _codeField.style.width = Length.Percent(100);
            _codeField.style.flexGrow = 1;
            _codeField.style.paddingTop = 0;
            _codeField.style.paddingBottom = 0;
            _codeField.style.paddingLeft = 0;
            _codeField.style.paddingRight = 0;
            _codeField.style.marginTop = 0;
            _codeField.style.marginBottom = 0;
            _codeField.style.marginLeft = 0;
            _codeField.style.marginRight = 0;
            _codeField.style.color = Color.white;
            _mainTextPanel.Add(_codeField);

            // Override _linesContainer styles: zero padding for full width, center vertically
            _linesContainer = _codeField.Q(className: "animated-code-field__lines");
            if (_linesContainer != null)
            {
                _linesContainer.style.paddingLeft = 0;
                _linesContainer.style.paddingTop = 0;
                _linesContainer.style.paddingRight = 0;
                _linesContainer.style.paddingBottom = 0;
                _linesContainer.style.justifyContent = Justify.Center;
            }

            // Wire input callbacks
            _codeField.RegisterValueChangedCallback(OnTextChanged);
            _codeField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            // Recalculate font size when panel resizes
            _mainTextPanel.RegisterCallback<GeometryChangedEvent>(_ => RecalculateFontSize());

            // Stats bar
            _statsBar = new VisualElement();
            _statsBar.style.flexGrow = 1;
            _statsBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _statsBar.style.flexDirection = FlexDirection.Row;
            _statsBar.style.alignItems = Align.Center;
            _statsBar.style.paddingLeft = 10;
            _statsBar.style.paddingRight = 10;
            leftColumn.Add(_statsBar);

            var hpLabel = new Label("HP: 100/100");
            hpLabel.style.color = Color.green;
            hpLabel.style.marginRight = 20;
            _statsBar.Add(hpLabel);

            var statsLabel = new Label("STR: 10  DEX: 8  INT: 12");
            statsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            statsLabel.style.marginRight = 20;
            _statsBar.Add(statsLabel);

            var statusLabel = new Label("Status: Normal");
            statusLabel.style.color = new Color(0.6f, 0.8f, 1f);
            _statsBar.Add(statusLabel);

            // --- Right Column: Tile Map ---
            var tileMapPanel = new VisualElement();
            tileMapPanel.style.flexGrow = 1;
            tileMapPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            tileMapPanel.style.flexDirection = FlexDirection.ColumnReverse;
            tileMapPanel.style.paddingTop = 4;
            tileMapPanel.style.paddingBottom = 4;
            tileMapPanel.style.paddingLeft = 4;
            tileMapPanel.style.paddingRight = 4;
            root.Add(tileMapPanel);

            BuildGridVisual(tileMapPanel);

            // Focus the code field after a frame so it's attached to the panel
            _codeField.schedule.Execute(() => _codeField.Focus());
        }

        private void BuildGridVisual(VisualElement container)
        {
            _gridCells.Clear();

            for (int y = 0; y < _gridHeight; y++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexGrow = 1;
                container.Add(row);

                for (int x = 0; x < _gridWidth; x++)
                {
                    var cell = new VisualElement();
                    cell.style.flexGrow = 1;
                    cell.style.borderTopWidth = 1;
                    cell.style.borderBottomWidth = 1;
                    cell.style.borderLeftWidth = 1;
                    cell.style.borderRightWidth = 1;
                    cell.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f);
                    cell.style.borderBottomColor = new Color(0.3f, 0.3f, 0.35f);
                    cell.style.borderLeftColor = new Color(0.3f, 0.3f, 0.35f);
                    cell.style.borderRightColor = new Color(0.3f, 0.3f, 0.35f);
                    cell.style.marginTop = 1;
                    cell.style.marginBottom = 1;
                    cell.style.marginLeft = 1;
                    cell.style.marginRight = 1;

                    var pos = new GridPosition(x, y);
                    var value = _grid.Get(pos);
                    cell.style.backgroundColor = value == 1
                        ? new Color(0.2f, 0.7f, 0.3f)
                        : new Color(0.18f, 0.18f, 0.22f);

                    row.Add(cell);
                    _gridCells.Add(cell);
                }
            }
        }

        private void UpdateGridCellVisual(GridPosition pos)
        {
            var index = pos.Y * _gridWidth + pos.X;
            if (index < 0 || index >= _gridCells.Count) return;

            var value = _grid.Get(pos);
            _gridCells[index].style.backgroundColor = value == 1
                ? new Color(0.2f, 0.7f, 0.3f)
                : new Color(0.18f, 0.18f, 0.22f);
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            var newText = evt.newValue ?? "";

            // Sync service state
            _service.Clear();
            foreach (var c in newText)
                _service.AppendCharacter(c);

            RecalculateFontSize();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                var word = _codeField.value?.Trim() ?? "";
                if (word.Length > 0)
                {
                    _service.Clear();
                    _eventBus.Publish(new WordSubmittedEvent(word));
                    _codeField.value = "";
                    RecalculateFontSize();
                }
                // Re-focus after a frame to ensure the hidden input stays active
                _codeField.schedule.Execute(() => _codeField.Focus());
                evt.StopImmediatePropagation();
            }
        }

        private void RecalculateFontSize()
        {
            if (_codeField == null || _mainTextPanel == null) return;

            // Use the actual _linesContainer width (where text renders) instead of the panel width
            var widthSource = _linesContainer ?? (VisualElement)_mainTextPanel;
            var panelWidth = widthSource.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0) return;

            var text = _codeField.value ?? "";
            var charCount = Mathf.Max(text.Length, 1);

            // First pass: estimate using the "X" baseline ratio
            var ratio = _codeField.BaseCharWidthRatio;
            var fontSize = panelWidth / (charCount * ratio);
            fontSize = Mathf.Clamp(fontSize, 12f, 800f);
            _codeField.SetCharFontSize(fontSize);

            // Second pass: wait for layout to settle, then measure and correct
            if (charCount > 0 && text.Length > 0)
            {
                var labels = _codeField.CharLabels;
                if (labels.Count == 0) return;

                // Use GeometryChangedEvent to ensure layout has actually happened
                // before measuring widths (schedule.Execute can fire before layout)
                EventCallback<GeometryChangedEvent> correctionCallback = null;
                correctionCallback = _ =>
                {
                    labels[0].UnregisterCallback(correctionCallback);
                    if (_codeField == null || _mainTextPanel == null) return;

                    var currentLabels = _codeField.CharLabels;
                    if (currentLabels.Count == 0) return;

                    float totalWidth = 0;
                    foreach (var label in currentLabels)
                    {
                        var w = label.resolvedStyle.width;
                        if (!float.IsNaN(w) && w > 0) totalWidth += w;
                    }

                    if (totalWidth <= 0) return;

                    var targetWidth = panelWidth * _fontScaleFactor;
                    var correction = targetWidth / totalWidth;
                    var correctedSize = fontSize * correction;
                    correctedSize = Mathf.Clamp(correctedSize, 12f, 800f);
                    _codeField.SetCharFontSize(correctedSize);
                };
                labels[0].RegisterCallback(correctionCallback);
            }
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var expectedCellCount = _gridWidth * _gridHeight;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _codeField != null,
                    _codeField != null ? null : "Code field is null"),
                new($"Grid cells spawned ({expectedCellCount})",
                    _gridCells.Count == expectedCellCount,
                    _gridCells.Count == expectedCellCount
                        ? null : $"Expected {expectedCellCount}, got {_gridCells.Count}"),
                new("Stats bar exists", _statsBar != null,
                    _statsBar != null ? null : "Stats bar is null"),
                new("Main text panel has black background",
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black,
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black
                        ? null : "Main text panel background is not black")
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _gridCells.Clear();

            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
            _service = null;
            _codeField = null;
            _linesContainer = null;
            _mainTextPanel = null;
            _statsBar = null;
            _grid = null;
        }
    }
}
