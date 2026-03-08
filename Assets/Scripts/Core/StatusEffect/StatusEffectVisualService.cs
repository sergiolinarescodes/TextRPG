using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.UnitRendering;
using Unidad.Core.EventBus;
using Unidad.Core.Systems;
using Unidad.Core.UI.TextAnimation;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.StatusEffect
{
    internal sealed class StatusEffectVisualService : SystemServiceBase
    {
        private StatusEffectFloatingTextPool _pool;
        private Func<EntityId, Vector3> _positionProvider;
        private bool _initialized;
        private TextAnimationService _textAnimService;

        private IReadOnlyList<VisualElement> _slotElements;
        private ICombatSlotService _slotService;
        private IStatusEffectService _statusEffects;
        private IUnitService _unitService;
        private IPassiveService _passiveService;
        private CombatSlotVisual _slotVisual;
        private VisualElement _tooltipLayer;
        private VisualElement _currentTooltip;
        private readonly List<(VisualElement cell, EventCallback<PointerEnterEvent> enter, EventCallback<PointerLeaveEvent> leave)> _hoverCallbacks = new();

        public StatusEffectVisualService(IEventBus eventBus) : base(eventBus)
        {
            Subscribe<StatusEffectAppliedEvent>(OnEffectApplied);
            Subscribe<StatusEffectDamageEvent>(OnEffectDamage);
            Subscribe<StatusEffectExpiredEvent>(OnEffectExpired);
            Subscribe<DamageTakenEvent>(OnDamageTaken);
        }

        public void Initialize(
            Func<EntityId, Vector3> positionProvider,
            VisualElement floatingTextLayer,
            IReadOnlyList<VisualElement> slotElements,
            ICombatSlotService slotService,
            IStatusEffectService statusEffects,
            IUnitService unitService,
            CombatSlotVisual slotVisual,
            VisualElement tooltipLayer,
            IPassiveService passiveService = null)
        {
            _positionProvider = positionProvider;
            _slotElements = slotElements;
            _slotService = slotService;
            _statusEffects = statusEffects;
            _unitService = unitService;
            _passiveService = passiveService;
            _slotVisual = slotVisual;
            _tooltipLayer = tooltipLayer;

            _textAnimService = new TextAnimationService();
            _pool = new StatusEffectFloatingTextPool(_textAnimService);
            _pool.Initialize(floatingTextLayer);

            RegisterHoverCallbacks();
            _initialized = true;
        }

        private void OnEffectApplied(StatusEffectAppliedEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), def.DisplayName, def.DisplayColor, "heal");
        }

        private void OnEffectDamage(StatusEffectDamageEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), $"-{e.Damage}", def.DisplayColor, "damage");
        }

        private void OnEffectExpired(StatusEffectExpiredEvent e)
        {
            if (!_initialized) return;
            var def = StatusEffectDefinitions.Get(e.Type);
            var pos = _positionProvider(e.Target);
            _pool.Spawn(new Vector2(pos.x, pos.y), $"{def.DisplayName} ENDED", new Color(0.6f, 0.6f, 0.6f), "heal");
        }

        private void OnDamageTaken(DamageTakenEvent e)
        {
            if (!_initialized || _slotVisual == null) return;

            var recipe = _textAnimService.GetRecipe("damage");
            _slotVisual.PlayHitAnimation(e.EntityId, recipe?.Duration ?? 1.5f, recipe?.MarkupTemplate ?? "<shake a=0.1 f=5>{0}</shake>");
        }

        private void RegisterHoverCallbacks()
        {
            for (int i = 0; i < _slotElements.Count; i++)
            {
                var cell = _slotElements[i];
                var cellIndex = i;

                EventCallback<PointerEnterEvent> enter = _ => OnCellPointerEnter(cellIndex);
                EventCallback<PointerLeaveEvent> leave = _ => OnCellPointerLeave();

                cell.RegisterCallback(enter);
                cell.RegisterCallback(leave);
                _hoverCallbacks.Add((cell, enter, leave));
            }
        }

        private void OnCellPointerEnter(int cellIndex)
        {
            HideTooltip();

            if (cellIndex < 0 || cellIndex >= _slotElements.Count) return;
            var cell = _slotElements[cellIndex];

            // Determine which entity is at this slot element
            // First 3 = enemy slots 0-2, next 2 = ally slots 0-1
            EntityId? entityId = null;
            if (cellIndex < 3)
                entityId = _slotService.GetEntityAt(SlotType.Enemy, cellIndex);
            else
                entityId = _slotService.GetEntityAt(SlotType.Ally, cellIndex - 3);

            if (!entityId.HasValue) return;

            var effects = _statusEffects.GetEffects(entityId.Value);

            var cellBound = cell.worldBound;

            var tooltip = new VisualElement();
            tooltip.style.position = Position.Absolute;
            tooltip.style.backgroundColor = Color.black;
            tooltip.style.borderTopWidth = 1;
            tooltip.style.borderBottomWidth = 1;
            tooltip.style.borderLeftWidth = 1;
            tooltip.style.borderRightWidth = 1;
            tooltip.style.borderTopColor = Color.white;
            tooltip.style.borderBottomColor = Color.white;
            tooltip.style.borderLeftColor = Color.white;
            tooltip.style.borderRightColor = Color.white;
            tooltip.style.paddingLeft = 16;
            tooltip.style.paddingRight = 16;
            tooltip.style.paddingTop = 12;
            tooltip.style.paddingBottom = 12;
            tooltip.pickingMode = PickingMode.Ignore;

            string unitName = entityId.Value.Value;
            var unitId = new UnitRendering.UnitId(entityId.Value.Value);
            if (_unitService != null && _unitService.TryGetUnit(unitId, out var unitInstance))
            {
                unitName = unitInstance.Definition.Name;
            }

            var nameLabel = new Label(unitName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 26;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.marginBottom = 8;
            nameLabel.pickingMode = PickingMode.Ignore;
            tooltip.Add(nameLabel);

            var passives = _passiveService?.GetPassives(entityId.Value);
            bool hasEffects = effects.Count > 0;
            bool hasPassives = passives != null && passives.Count > 0;

            if (!hasEffects && !hasPassives)
            {
                var noEffects = new Label("No active effects");
                noEffects.style.color = new Color(0.6f, 0.6f, 0.6f);
                noEffects.style.fontSize = 24;
                noEffects.pickingMode = PickingMode.Ignore;
                tooltip.Add(noEffects);
            }

            if (hasEffects)
            {
                foreach (var instance in effects)
                {
                    var def = StatusEffectDefinitions.Get(instance.Type);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 4;
                    row.pickingMode = PickingMode.Ignore;

                    var effectName = new Label(def.DisplayName);
                    effectName.style.color = def.DisplayColor;
                    effectName.style.unityFontStyleAndWeight = FontStyle.Bold;
                    effectName.style.fontSize = 24;
                    effectName.style.marginRight = 16;
                    effectName.pickingMode = PickingMode.Ignore;
                    row.Add(effectName);

                    var desc = new Label(def.Description);
                    desc.style.color = Color.white;
                    desc.style.fontSize = 24;
                    desc.pickingMode = PickingMode.Ignore;
                    row.Add(desc);

                    var infoText = instance.IsPermanent
                        ? $"permanent x{instance.StackCount}"
                        : $"({instance.RemainingDuration}t) x{instance.StackCount}";
                    var info = new Label(infoText);
                    info.style.color = new Color(0.6f, 0.6f, 0.6f);
                    info.style.fontSize = 24;
                    info.style.marginLeft = 16;
                    info.pickingMode = PickingMode.Ignore;
                    row.Add(info);

                    tooltip.Add(row);
                }
            }

            if (hasPassives)
            {
                var passiveHeader = new Label("Passives");
                passiveHeader.style.color = new Color(0f, 0.9f, 0.9f);
                passiveHeader.style.fontSize = 22;
                passiveHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                passiveHeader.style.marginTop = hasEffects ? 8 : 0;
                passiveHeader.style.marginBottom = 4;
                passiveHeader.pickingMode = PickingMode.Ignore;
                tooltip.Add(passiveHeader);

                foreach (var entry in passives)
                {
                    var pDef = PassiveDefinitions.Get(entry.PassiveId);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 4;
                    row.pickingMode = PickingMode.Ignore;

                    var pName = new Label(pDef.DisplayName);
                    pName.style.color = pDef.DisplayColor;
                    pName.style.unityFontStyleAndWeight = FontStyle.Bold;
                    pName.style.fontSize = 24;
                    pName.style.marginRight = 16;
                    pName.pickingMode = PickingMode.Ignore;
                    row.Add(pName);

                    var pDesc = new Label(pDef.Description);
                    pDesc.style.color = Color.white;
                    pDesc.style.fontSize = 24;
                    pDesc.pickingMode = PickingMode.Ignore;
                    row.Add(pDesc);

                    var pValue = new Label($"({entry.Value})");
                    pValue.style.color = new Color(0.6f, 0.6f, 0.6f);
                    pValue.style.fontSize = 24;
                    pValue.style.marginLeft = 16;
                    pValue.pickingMode = PickingMode.Ignore;
                    row.Add(pValue);

                    tooltip.Add(row);
                }
            }

            _tooltipLayer.Add(tooltip);
            _currentTooltip = tooltip;

            // Position after layout using framework TooltipPositioner for clamping
            tooltip.RegisterCallback<GeometryChangedEvent>(OnTooltipLayout);

            void OnTooltipLayout(GeometryChangedEvent evt)
            {
                tooltip.UnregisterCallback<GeometryChangedEvent>(OnTooltipLayout);
                var tooltipSize = new Vector2(tooltip.resolvedStyle.width, tooltip.resolvedStyle.height);
                var containerSize = new Vector2(_tooltipLayer.resolvedStyle.width, _tooltipLayer.resolvedStyle.height);
                var result = TooltipPositioner.Compute(cellBound, tooltipSize, containerSize, TooltipPlacement.Bottom, arrowSize: 0f);
                tooltip.style.left = result.Position.x;
                tooltip.style.top = result.Position.y;
            }
        }

        private void OnCellPointerLeave()
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            _currentTooltip?.RemoveFromHierarchy();
            _currentTooltip = null;
        }

        public override void Dispose()
        {
            HideTooltip();

            foreach (var (cell, enter, leave) in _hoverCallbacks)
            {
                cell.UnregisterCallback(enter);
                cell.UnregisterCallback(leave);
            }
            _hoverCallbacks.Clear();

            _pool?.Dispose();
            _pool = null;
            _textAnimService?.Dispose();
            _textAnimService = null;
            _initialized = false;
            base.Dispose();
        }
    }
}
