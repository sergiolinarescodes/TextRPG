using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class EquipmentBarVisual
    {
        private readonly int _slotCount;
        private readonly VisualElement[] _slotCells;
        private readonly VisualElement[] _backgroundContainers;
        private readonly VisualElement[] _contentContainers;

        public EquipmentBarVisual(int slotCount)
        {
            _slotCount = slotCount;
            _slotCells = new VisualElement[slotCount];
            _backgroundContainers = new VisualElement[slotCount];
            _contentContainers = new VisualElement[slotCount];
        }

        public void BuildColumn(VisualElement parent)
        {
            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.justifyContent = Justify.Center;
            column.style.alignItems = Align.Center;
            column.style.flexShrink = 0;
            column.style.backgroundColor = Color.black;

            for (int i = 0; i < _slotCount; i++)
            {
                var cell = CreateSlotCell();
                column.Add(cell);
                _slotCells[i] = cell;

                var bg = new VisualElement();
                bg.style.position = Position.Absolute;
                bg.style.top = 0;
                bg.style.left = 0;
                bg.style.right = 0;
                bg.style.bottom = 0;
                bg.style.flexDirection = FlexDirection.Column;
                bg.style.justifyContent = Justify.Center;
                bg.style.alignItems = Align.Center;
                bg.style.overflow = Overflow.Hidden;
                cell.Add(bg);
                _backgroundContainers[i] = bg;

                var content = new VisualElement();
                content.style.position = Position.Absolute;
                content.style.top = 0;
                content.style.left = 0;
                content.style.right = 0;
                content.style.bottom = 0;
                content.style.flexDirection = FlexDirection.Column;
                content.style.justifyContent = Justify.Center;
                content.style.alignItems = Align.Center;
                content.style.overflow = Overflow.Hidden;
                content.style.display = DisplayStyle.None;
                cell.Add(content);
                _contentContainers[i] = content;
            }

            parent.Add(column);
        }

        public void SetSlotBackground(int index, string text, Color color)
        {
            if (index < 0 || index >= _slotCount) return;
            var bg = _backgroundContainers[index];
            bg.Clear();

            var cell = _slotCells[index];
            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 120f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 100f;

            var layout = UnitTextLayout.Calculate(text, cellWidth, cellHeight);
            UnitTextLabels.AddTo(layout, color, bg);
        }

        public void SetSlotContent(int index, string displayName, Color textColor)
        {
            if (index < 0 || index >= _slotCount) return;
            var content = _contentContainers[index];
            content.Clear();
            content.style.display = DisplayStyle.Flex;
            content.style.backgroundColor = Color.black;

            var cell = _slotCells[index];
            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 120f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 100f;

            var layout = UnitTextLayout.Calculate(displayName.ToUpperInvariant(), cellWidth, cellHeight);
            UnitTextLabels.AddTo(layout, textColor, content);
        }

        public void ClearSlotContent(int index)
        {
            if (index < 0 || index >= _slotCount) return;
            var content = _contentContainers[index];
            content.Clear();
            content.style.display = DisplayStyle.None;
        }

        public VisualElement GetSlotElement(int index)
        {
            if (index < 0 || index >= _slotCount) return null;
            return _slotCells[index];
        }

        public IReadOnlyList<VisualElement> GetAllSlotElements()
        {
            return _slotCells;
        }

        private static VisualElement CreateSlotCell()
        {
            var cell = new VisualElement();
            cell.style.width = 120;
            cell.style.height = 100;
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
            cell.style.marginLeft = 4;
            cell.style.marginRight = 4;
            cell.style.marginTop = 4;
            cell.style.marginBottom = 4;
            return cell;
        }
    }
}
