using System;
using System.Collections.Generic;
using TextRPG.Core.Equipment;
using TextRPG.Core.Scroll;
using Unidad.Core.EventBus;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class LootOverlayController : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ILootRewardService _lootRewardService;
        private readonly EquipmentBarVisual _rightBar;
        private readonly VisualElement _overlayParent;
        private readonly Action<bool> _setInputEnabled;
        private readonly List<IDisposable> _subscriptions = new();

        private VisualElement _lootOverlay;
        private VisualElement _lootTooltip;
        private int _highlightedSlotIndex = -1;

        public LootOverlayController(IEventBus eventBus, ILootRewardService lootRewardService,
            EquipmentBarVisual rightBar, VisualElement overlayParent, Action<bool> setInputEnabled)
        {
            _eventBus = eventBus;
            _lootRewardService = lootRewardService;
            _rightBar = rightBar;
            _overlayParent = overlayParent;
            _setInputEnabled = setInputEnabled;

            _subscriptions.Add(_eventBus.Subscribe<LootRewardOfferedEvent>(evt => ShowLootSelection(evt.Options)));
            _subscriptions.Add(_eventBus.Subscribe<LootRewardSelectedEvent>(_ => HideLootSelection()));
        }

        private void ShowLootSelection(LootRewardOption[] options)
        {
            _setInputEnabled(false);

            _lootOverlay = new VisualElement();
            _lootOverlay.style.position = Position.Absolute;
            _lootOverlay.style.left = 0;
            _lootOverlay.style.top = 0;
            _lootOverlay.style.right = 0;
            _lootOverlay.style.bottom = 0;
            _lootOverlay.style.justifyContent = Justify.Center;
            _lootOverlay.style.alignItems = Align.Center;
            _lootOverlay.pickingMode = PickingMode.Position;

            var title = new Label("Choose a reward");
            title.style.fontSize = 32;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 24;
            title.pickingMode = PickingMode.Ignore;
            _lootOverlay.Add(title);

            var cardRow = new VisualElement();
            cardRow.style.flexDirection = FlexDirection.Row;
            cardRow.style.justifyContent = Justify.Center;
            cardRow.style.alignItems = Align.FlexStart;
            cardRow.pickingMode = PickingMode.Ignore;
            _lootOverlay.Add(cardRow);

            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var cardIndex = i;

                var card = new VisualElement();
                card.style.marginLeft = 12;
                card.style.marginRight = 12;
                card.style.alignItems = Align.Center;
                card.pickingMode = PickingMode.Position;

                card.Add(TooltipContentBuilder.CreateMiniWordBox(option.DisplayName, option.Color, 120, 100));

                card.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    ShowLootTooltip(option, card);
                    if (!option.IsScroll)
                        HighlightEquipmentSlot((int)option.Equipment.SlotType);
                });
                card.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    HideLootTooltip();
                    ClearEquipmentSlotHighlight();
                });
                card.RegisterCallback<ClickEvent>(_ => _lootRewardService.SelectReward(cardIndex));

                cardRow.Add(card);
            }

            _overlayParent.Add(_lootOverlay);
        }

        private void HideLootSelection()
        {
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
            ClearEquipmentSlotHighlight();
            _setInputEnabled(true);
        }

        private void ShowLootTooltip(LootRewardOption option, VisualElement card)
        {
            HideLootTooltip();

            _lootTooltip = new VisualElement();
            _lootTooltip.style.position = Position.Absolute;
            _lootTooltip.style.backgroundColor = Color.black;
            _lootTooltip.style.borderTopWidth = 1;
            _lootTooltip.style.borderBottomWidth = 1;
            _lootTooltip.style.borderLeftWidth = 1;
            _lootTooltip.style.borderRightWidth = 1;
            _lootTooltip.style.borderTopColor = Color.white;
            _lootTooltip.style.borderBottomColor = Color.white;
            _lootTooltip.style.borderLeftColor = Color.white;
            _lootTooltip.style.borderRightColor = Color.white;
            _lootTooltip.style.paddingLeft = 16;
            _lootTooltip.style.paddingRight = 16;
            _lootTooltip.style.paddingTop = 12;
            _lootTooltip.style.paddingBottom = 12;
            _lootTooltip.pickingMode = PickingMode.Ignore;

            if (option.IsScroll)
            {
                var scroll = option.Scroll;
                _lootTooltip.Add(TooltipContentBuilder.BuildHeader(scroll.DisplayName, scroll.Color, "SCROLL"));
                _lootTooltip.Add(TooltipContentBuilder.BuildScrollContent(scroll));
            }
            else
            {
                var item = option.Equipment;
                _lootTooltip.Add(TooltipContentBuilder.BuildHeader(item.DisplayName, item.Color, item.SlotType.ToString()));
                _lootTooltip.Add(TooltipContentBuilder.BuildArmorContent(item));
            }

            _overlayParent.Add(_lootTooltip);

            var cardBound = card.worldBound;
            _lootTooltip.style.left = cardBound.x + cardBound.width + 12;
            _lootTooltip.style.top = cardBound.y;
        }

        private void HideLootTooltip()
        {
            _lootTooltip?.RemoveFromHierarchy();
            _lootTooltip = null;
        }

        private void HighlightEquipmentSlot(int slotIndex)
        {
            ClearEquipmentSlotHighlight();
            _highlightedSlotIndex = slotIndex;
            var slot = _rightBar.GetSlotElement(slotIndex);
            var green = new Color(0.3f, 1f, 0.3f);
            slot.style.borderTopColor = green;
            slot.style.borderBottomColor = green;
            slot.style.borderLeftColor = green;
            slot.style.borderRightColor = green;
        }

        private void ClearEquipmentSlotHighlight()
        {
            if (_highlightedSlotIndex < 0) return;
            var slot = _rightBar.GetSlotElement(_highlightedSlotIndex);
            slot.style.borderTopColor = Color.white;
            slot.style.borderBottomColor = Color.white;
            slot.style.borderLeftColor = Color.white;
            slot.style.borderRightColor = Color.white;
            _highlightedSlotIndex = -1;
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
            ClearEquipmentSlotHighlight();
        }
    }
}
