using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.Grid;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class UnitGridVisual
    {
        private readonly IGrid<UnitId?> _grid;
        private readonly IUnitService _unitService;
        private readonly IEntityStatsService _entityStats;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly HashSet<string> _excludedHealthBarUnitIds;
        private readonly List<VisualElement> _cells = new();
        private readonly List<List<VisualElement>> _cellLabels = new();

        public IReadOnlyList<VisualElement> Cells => _cells;

        public UnitGridVisual(IGrid<UnitId?> grid, IUnitService unitService, IEntityStatsService entityStats,
                              int gridWidth, int gridHeight, HashSet<string> excludedHealthBarUnitIds = null)
        {
            _grid = grid;
            _unitService = unitService;
            _entityStats = entityStats;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _excludedHealthBarUnitIds = excludedHealthBarUnitIds;
        }

        public void Build(VisualElement container)
        {
            _cells.Clear();
            _cellLabels.Clear();

            var cellWidthPct = 100f / _gridWidth;
            var cellHeightPct = 100f / _gridHeight;

            for (int y = 0; y < _gridHeight; y++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = Length.Percent(cellHeightPct);
                row.style.flexShrink = 0;
                container.Add(row);

                for (int x = 0; x < _gridWidth; x++)
                {
                    var cell = CreateCell(cellWidthPct);
                    row.Add(cell);
                    _cells.Add(cell);
                    _cellLabels.Add(new List<VisualElement>());

                    RenderCellContent(new GridPosition(x, y));
                }
            }
        }

        public void UpdateCell(GridPosition pos)
        {
            var index = pos.Y * _gridWidth + pos.X;
            if (index < 0 || index >= _cells.Count) return;
            RenderCellContent(pos);
        }

        public void RefreshFontSizes()
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    var index = y * _gridWidth + x;
                    if (index >= _cells.Count) continue;

                    var cell = _cells[index];
                    var labels = _cellLabels[index];
                    if (labels.Count == 0) continue;

                    var unitId = _grid.Get(new GridPosition(x, y));
                    if (unitId == null) continue;
                    if (!_unitService.TryGetUnit(unitId.Value, out var unit)) continue;

                    float cellWidth = cell.resolvedStyle.width;
                    float cellHeight = cell.resolvedStyle.height;
                    if (float.IsNaN(cellWidth) || cellWidth <= 0) continue;
                    if (float.IsNaN(cellHeight) || cellHeight <= 0) continue;

                    var layout = UnitTextLayout.Calculate(unit.Definition.Name, cellWidth, cellHeight);
                    foreach (var label in labels)
                        label.style.fontSize = layout.FontSize;
                }
            }
        }

        public void HighlightCells(IReadOnlyList<GridPosition> positions, Color color)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                var index = pos.Y * _gridWidth + pos.X;
                if (index >= 0 && index < _cells.Count)
                    _cells[index].style.backgroundColor = color;
            }
        }

        public void PlayHitAnimation(GridPosition pos, float durationSeconds = 1.5f, string markupTemplate = "<shake a=0.1 f=5>{0}</shake>")
        {
            var index = pos.Y * _gridWidth + pos.X;
            if (index < 0 || index >= _cells.Count) return;

            var unitId = _grid.Get(pos);
            if (unitId == null) return;
            if (!_unitService.TryGetUnit(unitId.Value, out var unit)) return;

            var cell = _cells[index];
            var labels = _cellLabels[index];

            // Remove existing text labels (keep health bars etc.)
            foreach (var label in labels)
                label.RemoveFromHierarchy();
            labels.Clear();

            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 60f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 60f;

            var layout = UnitTextLayout.Calculate(unit.Definition.Name, cellWidth, cellHeight);
            var animatedLabels = UnitTextLabels.AddAnimatedWithRecipeTo(layout, unit.Definition.Color, cell, markupTemplate);
            // Insert animated labels before health bars
            for (int i = animatedLabels.Length - 1; i >= 0; i--)
            {
                cell.Remove(animatedLabels[i]);
                cell.Insert(0, animatedLabels[i]);
            }

            labels.AddRange(animatedLabels);

            // Schedule revert to normal labels
            cell.schedule.Execute(() => UpdateCell(pos)).ExecuteLater((long)(durationSeconds * 1000));
        }

        public void ClearHighlights()
        {
            for (int i = 0; i < _cells.Count; i++)
                _cells[i].style.backgroundColor = Color.black;
        }

        private void RenderCellContent(GridPosition pos)
        {
            var index = pos.Y * _gridWidth + pos.X;
            var cell = _cells[index];
            var labels = _cellLabels[index];

            cell.Clear();
            labels.Clear();
            cell.style.backgroundColor = Color.black;

            var unitId = _grid.Get(pos);
            if (unitId == null) return;
            if (!_unitService.TryGetUnit(unitId.Value, out var unit)) return;

            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 60f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 60f;

            var layout = UnitTextLayout.Calculate(unit.Definition.Name, cellWidth, cellHeight);
            labels.AddRange(UnitTextLabels.AddTo(layout, unit.Definition.Color, cell));

            var entityId = new EntityStats.EntityId(unit.Id.Value);
            bool excluded = _excludedHealthBarUnitIds != null && _excludedHealthBarUnitIds.Contains(unit.Id.Value);
            if (!excluded && _entityStats != null && _entityStats.HasEntity(entityId))
            {
                var barContainer = new VisualElement();
                barContainer.style.position = Position.Absolute;
                barContainer.style.bottom = 0;
                barContainer.style.left = 0;
                barContainer.style.right = 0;
                barContainer.style.height = 14;
                barContainer.style.flexDirection = FlexDirection.Column;
                barContainer.style.justifyContent = Justify.FlexEnd;

                int currentShield = _entityStats.GetCurrentShield(entityId);
                int currentHealth = _entityStats.GetCurrentHealth(entityId);
                int maxHealth = _entityStats.GetStat(entityId, StatType.MaxHealth);

                if (currentShield > 0)
                {
                    var shieldBar = new VisualElement();
                    float shieldPct = (float)currentShield / (maxHealth + currentShield) * 100f;
                    shieldBar.style.width = Length.Percent(shieldPct);
                    shieldBar.style.height = 4;
                    shieldBar.style.backgroundColor = Color.grey;
                    barContainer.Add(shieldBar);

                    var shieldLabel = new Label(currentShield.ToString());
                    shieldLabel.style.fontSize = 7;
                    shieldLabel.style.color = Color.grey;
                    shieldLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    shieldLabel.style.position = Position.Absolute;
                    shieldLabel.style.bottom = 10;
                    shieldLabel.style.left = 0;
                    shieldLabel.style.right = 0;
                    shieldLabel.style.height = 10;
                    cell.Add(shieldLabel);
                }

                var healthBar = new VisualElement();
                float healthPct = maxHealth > 0 ? (float)currentHealth / maxHealth * 100f : 0f;
                healthBar.style.width = Length.Percent(healthPct);
                healthBar.style.height = currentShield > 0 ? 3 : 4;
                healthBar.style.backgroundColor = Color.green;
                barContainer.Add(healthBar);

                cell.Add(barContainer);
            }
        }

        private static VisualElement CreateCell(float widthPct)
        {
            var cell = new VisualElement();
            cell.style.width = Length.Percent(widthPct);
            cell.style.height = Length.Percent(100);
            cell.style.flexShrink = 0;
            cell.style.flexGrow = 0;
            cell.style.flexDirection = FlexDirection.Column;
            cell.style.justifyContent = Justify.Center;
            cell.style.alignItems = Align.Center;
            cell.style.overflow = Overflow.Hidden;
            cell.style.borderTopWidth = 1;
            cell.style.borderBottomWidth = 1;
            cell.style.borderLeftWidth = 1;
            cell.style.borderRightWidth = 1;
            cell.style.borderTopColor = Color.white;
            cell.style.borderBottomColor = Color.white;
            cell.style.borderLeftColor = Color.white;
            cell.style.borderRightColor = Color.white;
            cell.style.backgroundColor = Color.black;
            return cell;
        }
    }
}
