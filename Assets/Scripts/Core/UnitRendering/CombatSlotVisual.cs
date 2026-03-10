using System.Collections.Generic;
using PrimeTween;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using Unidad.Core.UI.TextAnimation;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    public static class SlotColors
    {
        public static readonly Color Placeholder = new(0.2f, 0.2f, 0.2f);
    }

    internal sealed class CombatSlotVisual
    {
        private readonly IUnitService _unitService;
        private readonly IEntityStatsService _entityStats;
        private readonly ICombatSlotService _slotService;
        private readonly Dictionary<EntityId, VisualElement> _slotElements = new();
        private readonly List<VisualElement> _allSlotElements = new();
        private readonly HashSet<VisualElement> _allyCells = new();

        public CombatSlotVisual(IUnitService unitService, IEntityStatsService entityStats, ICombatSlotService slotService)
        {
            _unitService = unitService;
            _entityStats = entityStats;
            _slotService = slotService;
        }

        public void BuildEnemyRow(VisualElement container)
        {
            for (int i = 0; i < 3; i++)
            {
                var cell = CreateSlotCell();
                container.Add(cell);
                _allSlotElements.Add(cell);

                var entity = _slotService.GetEntityAt(SlotType.Enemy, i);
                if (entity.HasValue)
                {
                    _slotElements[entity.Value] = cell;
                    RenderSlotContent(cell, entity.Value);
                }
            }
        }

        public void BuildAllyRow(VisualElement container)
        {
            for (int i = 0; i < 2; i++)
            {
                var cell = CreateSlotCell();
                container.Add(cell);
                _allSlotElements.Add(cell);
                _allyCells.Add(cell);

                var entity = _slotService.GetEntityAt(SlotType.Ally, i);
                if (entity.HasValue)
                {
                    _slotElements[entity.Value] = cell;
                    RenderSlotContent(cell, entity.Value);
                }
                else
                {
                    RenderPlaceholder(cell, "SUMMON");
                }
            }
        }

        private static void RenderPlaceholder(VisualElement cell, string text)
        {
            cell.Clear();
            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 120f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 100f;

            var layout = UnitTextLayout.Calculate(text, cellWidth, cellHeight);
            UnitTextLabels.AddTo(layout, SlotColors.Placeholder, cell);
        }

        public void RegisterEntity(EntityId entityId, VisualElement cell)
        {
            _slotElements[entityId] = cell;
            RenderSlotContent(cell, entityId);
        }

        public VisualElement GetSlotElement(EntityId entityId)
        {
            return _slotElements.TryGetValue(entityId, out var cell) ? cell : null;
        }

        public IReadOnlyList<VisualElement> GetAllSlotElements() => _allSlotElements;

        public void RefreshSlot(EntityId entityId)
        {
            if (!_slotElements.TryGetValue(entityId, out var cell)) return;
            RenderSlotContent(cell, entityId);
        }

        public void PlayHitAnimation(EntityId entityId, float durationSeconds = 1.5f, string markupTemplate = "<shake a=0.1 f=5>{0}</shake>")
        {
            if (!_slotElements.TryGetValue(entityId, out var cell)) return;

            var unitId = new UnitId(entityId.Value);
            if (!_unitService.TryGetUnit(unitId, out var unit)) return;

            cell.Clear();

            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 80f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 80f;

            var layout = UnitTextLayout.Calculate(unit.Definition.Name, cellWidth, cellHeight);
            var animatedLabels = UnitTextLabels.AddAnimatedWithRecipeTo(layout, unit.Definition.Color, cell, markupTemplate);

            for (int i = animatedLabels.Length - 1; i >= 0; i--)
            {
                cell.Remove(animatedLabels[i]);
                cell.Insert(0, animatedLabels[i]);
            }

            AddHealthBar(cell, entityId);

            cell.schedule.Execute(() => RefreshSlot(entityId)).ExecuteLater((long)(durationSeconds * 1000));
        }

        public void PlayDeathAnimation(EntityId entityId, float duration = 0.6f)
        {
            if (!_slotElements.TryGetValue(entityId, out var cell)) return;

            var children = new List<VisualElement>();
            for (int i = 0; i < cell.childCount; i++)
                children.Add(cell[i]);

            Tween.Custom(cell, 0f, 1f, duration,
                onValueChange: (c, progress) =>
                {
                    foreach (var child in children)
                    {
                        child.style.translate = new Translate(0, Length.Percent(progress * 120));
                        child.style.opacity = 1f - progress;
                    }
                },
                Ease.InQuad
            ).OnComplete(cell, c =>
            {
                c.Clear();
                c.style.backgroundColor = Color.black;
                if (_allyCells.Contains(c))
                    RenderPlaceholder(c, "SUMMON");
            });

            _slotElements.Remove(entityId);
        }

        private void RenderSlotContent(VisualElement cell, EntityId entityId)
        {
            cell.Clear();
            cell.style.backgroundColor = Color.black;

            var unitId = new UnitId(entityId.Value);
            if (!_unitService.TryGetUnit(unitId, out var unit))
            {
                if (!_entityStats.HasEntity(entityId)) return;
                var label = new Label(entityId.Value.ToUpperInvariant());
                label.style.color = Color.white;
                label.style.fontSize = 14;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                cell.Add(label);
                AddHealthBar(cell, entityId);
                return;
            }

            float cellWidth = cell.resolvedStyle.width;
            float cellHeight = cell.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 80f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 80f;

            var layout = UnitTextLayout.Calculate(unit.Definition.Name, cellWidth, cellHeight);
            UnitTextLabels.AddTo(layout, unit.Definition.Color, cell);

            AddHealthBar(cell, entityId);
        }

        private void AddHealthBar(VisualElement cell, EntityId entityId)
        {
            if (!_entityStats.HasEntity(entityId)) return;

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
            }

            var healthBar = new VisualElement();
            float healthPct = maxHealth > 0 ? (float)currentHealth / maxHealth * 100f : 0f;
            healthBar.style.width = Length.Percent(healthPct);
            healthBar.style.height = currentShield > 0 ? 3 : 4;
            healthBar.style.backgroundColor = Color.green;
            barContainer.Add(healthBar);

            int maxMana = _entityStats.GetStat(entityId, StatType.MaxMana);
            if (maxMana > 0)
            {
                int currentMana = _entityStats.GetCurrentMana(entityId);
                var manaBar = new VisualElement();
                float manaPct = (float)currentMana / maxMana * 100f;
                manaBar.style.width = Length.Percent(manaPct);
                manaBar.style.height = 3;
                manaBar.style.backgroundColor = new Color(0.3f, 0.5f, 1f);
                barContainer.Add(manaBar);
                barContainer.style.height = 18;
            }

            cell.Add(barContainer);
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
            return cell;
        }
    }
}
