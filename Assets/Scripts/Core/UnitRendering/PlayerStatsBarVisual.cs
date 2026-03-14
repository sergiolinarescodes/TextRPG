using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.PlayerClass;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Resource;
using Unidad.Core.UI.Components;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class PlayerStatsBarVisual : IDisposable
    {
        private static readonly (StatType Type, string Abbrev)[] DisplayStats =
        {
            (StatType.Strength, "STR"), (StatType.Dexterity, "DEX"), (StatType.MagicPower, "MGC"),
            (StatType.PhysicalDefense, "PDEF"), (StatType.MagicDefense, "MDEF"), (StatType.DamageReduction, "DRED"),
            (StatType.Luck, "LCK"), (StatType.Constitution, "CON"),
        };

        private readonly IEventBus _eventBus;
        private readonly IEntityStatsService _entityStats;
        private readonly IStatusEffectService _statusEffects;
        private readonly IResourceService _resourceService;
        private readonly IClassService _classService;
        private readonly ITooltipService _tooltipService;
        private readonly EntityId _playerId;
        private readonly Dictionary<StatType, Label> _statLabels = new();
        private readonly List<IDisposable> _subscriptions = new();

        private readonly List<IDisposable> _statusTooltipHandles = new();
        private VisualElement _statusRow;
        private VisualElement _statusEffectsContainer;
        private Label _goldLabel;
        private bool _isPreviewingManaCost;

        public VisualElement Root { get; private set; }
        public UnidadProgressBar HpBar { get; private set; }
        public Label HpLabel { get; private set; }
        public UnidadProgressBar ManaBar { get; private set; }
        public Label ManaLabel { get; private set; }
        public VisualElement ManaCostOverlay { get; private set; }
        public Label LevelLabel { get; private set; }
        public UnidadProgressBar XpBar { get; private set; }

        public PlayerStatsBarVisual(IEventBus eventBus, IEntityStatsService entityStats,
            IStatusEffectService statusEffects, EntityId playerId,
            IResourceService resourceService = null,
            IClassService classService = null, ITooltipService tooltipService = null)
        {
            _eventBus = eventBus;
            _entityStats = entityStats;
            _statusEffects = statusEffects;
            _resourceService = resourceService;
            _classService = classService;
            _tooltipService = tooltipService;
            _playerId = playerId;
        }

        public VisualElement Build()
        {
            Root = new VisualElement();
            Root.style.flexGrow = 0;
            Root.style.flexShrink = 0;
            Root.style.height = StyleKeyword.Auto;
            Root.style.backgroundColor = Color.black;
            Root.style.flexDirection = FlexDirection.Column;
            Root.style.justifyContent = Justify.Center;
            Root.style.paddingLeft = 10;
            Root.style.paddingRight = 10;
            Root.style.paddingTop = 4;
            Root.style.paddingBottom = 4;

            BuildBarsRow();
            BuildClassRow();
            BuildStatsAndStatusRow();
            BuildGoldDisplay();
            SubscribeToEvents();

            return Root;
        }

        public void UpdateHpBar()
        {
            if (HpBar == null || HpLabel == null) return;
            int hp = _entityStats.GetCurrentHealth(_playerId);
            int maxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            HpBar.Value = (float)hp / maxHp;
            HpLabel.text = $"{hp}/{maxHp}";
        }

        public void UpdateManaBar()
        {
            if (ManaBar == null || ManaLabel == null) return;
            int mana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            ManaBar.Value = maxMana > 0 ? (float)mana / maxMana : 0f;
            ManaLabel.text = $"{mana}/{maxMana}";
        }

        public void ShowManaCostPreview(IWordResolver wordResolver, string word)
        {
            if (ManaBar == null || ManaLabel == null || ManaCostOverlay == null) return;

            if (!WordPrefixHelper.TryStripGivePrefix(ref word) && word.Length == 0) { ClearManaCostPreview(); return; }

            var meta = wordResolver.GetStats(word);
            int cost = meta.Cost;
            if (cost <= 0) { ClearManaCostPreview(); return; }

            _isPreviewingManaCost = true;
            int currentMana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            if (maxMana <= 0) return;

            float currentRatio = (float)currentMana / maxMana;
            int previewMana = currentMana - cost;
            float previewRatio = (float)previewMana / maxMana;

            ManaBar.Value = Mathf.Clamp01(previewRatio);

            float overlayLeft = Mathf.Max(0f, previewRatio);
            float overlayWidth = currentRatio - overlayLeft;
            ManaCostOverlay.style.left = Length.Percent(overlayLeft * 100f);
            ManaCostOverlay.style.width = Length.Percent(overlayWidth * 100f);
            ManaCostOverlay.style.display = DisplayStyle.Flex;

            bool canAfford = previewMana >= 0;
            ManaCostOverlay.style.backgroundColor = canAfford
                ? new Color(1f, 0.6f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);

            ManaLabel.text = $"{previewMana}/{maxMana} (-{cost})";
            ManaLabel.style.color = canAfford ? Color.white : Color.red;
        }

        public void ClearManaCostPreview()
        {
            if (!_isPreviewingManaCost) return;
            _isPreviewingManaCost = false;
            if (ManaCostOverlay != null)
                ManaCostOverlay.style.display = DisplayStyle.None;
            UpdateManaBar();
            if (ManaLabel != null)
                ManaLabel.style.color = Color.white;
        }

        public bool IsPreviewingManaCost => _isPreviewingManaCost;

        public void FlashManaDanger()
        {
            ManaBar?.SetVariant(ProgressVariant.Danger);
            ManaBar?.schedule.Execute(() => ManaBar?.SetVariant(ProgressVariant.Info)).ExecuteLater(500);
        }

        public void UpdateLevelDisplay(int level, float xpProgress)
        {
            if (LevelLabel != null) LevelLabel.text = $"LVL {level}";
            if (XpBar != null) XpBar.Value = xpProgress;
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            foreach (var h in _statusTooltipHandles) h.Dispose();
            _statusTooltipHandles.Clear();
            _statLabels.Clear();
            _statusEffectsContainer = null;
            _statusRow = null;
            Root = null;
            HpBar = null;
            HpLabel = null;
            ManaBar = null;
            ManaLabel = null;
            ManaCostOverlay = null;
            LevelLabel = null;
            XpBar = null;
            _goldLabel = null;
        }

        private void BuildBarsRow()
        {
            var barsRow = new VisualElement();
            barsRow.style.flexDirection = FlexDirection.Row;
            barsRow.style.marginBottom = 4;

            // HP column (green)
            var hpCol = CreateBarColumn();
            HpBar = new UnidadProgressBar(1f);
            HpBar.SetVariant(ProgressVariant.Success);
            HpBar.style.width = Length.Percent(100);
            HpBar.style.height = 16;
            hpCol.Add(HpBar);

            int hp = _entityStats.GetCurrentHealth(_playerId);
            int maxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            HpLabel = CreateBarLabel($"{hp}/{maxHp}");
            hpCol.Add(HpLabel);

            // Mana column (blue)
            var manaCol = CreateBarColumn();
            ManaBar = new UnidadProgressBar(0.5f);
            ManaBar.SetVariant(ProgressVariant.Info);
            ManaBar.style.width = Length.Percent(100);
            ManaBar.style.height = 16;
            manaCol.Add(ManaBar);

            ManaCostOverlay = new VisualElement();
            ManaCostOverlay.style.position = Position.Absolute;
            ManaCostOverlay.style.top = 0;
            ManaCostOverlay.style.bottom = 0;
            ManaCostOverlay.style.display = DisplayStyle.None;
            ManaCostOverlay.pickingMode = PickingMode.Ignore;
            ManaBar.Add(ManaCostOverlay);

            int mana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            ManaLabel = CreateBarLabel($"{mana}/{maxMana}");
            manaCol.Add(ManaLabel);

            // Level column (white)
            var lvlCol = CreateBarColumn();
            XpBar = new UnidadProgressBar(0f);
            XpBar.style.width = Length.Percent(100);
            XpBar.style.height = 16;
            var fill = XpBar.Q("unidad-progress__fill");
            if (fill != null)
                fill.style.backgroundColor = Color.white;
            XpBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            lvlCol.Add(XpBar);

            LevelLabel = CreateBarLabel("LVL 1");
            lvlCol.Add(LevelLabel);

            // Order: Level (left), HP (center), Mana (right)
            barsRow.Add(lvlCol);
            barsRow.Add(hpCol);
            barsRow.Add(manaCol);

            Root.Add(barsRow);
        }

        private static VisualElement CreateBarColumn()
        {
            var col = new VisualElement();
            col.style.flexGrow = 1;
            col.style.flexBasis = 0;
            col.style.height = 32;
            col.style.marginLeft = 2;
            col.style.marginRight = 2;
            col.style.justifyContent = Justify.Center;
            return col;
        }

        private static Label CreateBarLabel(string text)
        {
            var label = new Label(text);
            label.style.position = Position.Absolute;
            label.style.top = 0;
            label.style.left = 0;
            label.style.right = 0;
            label.style.bottom = 0;
            label.style.fontSize = 24;
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        private void BuildClassRow()
        {
            if (_classService == null) return;

            var def = _classService.Definition;
            var classLabel = new Label($"CLASS: {def.DisplayName.ToUpperInvariant()}");
            classLabel.style.color = def.Color;
            classLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            classLabel.style.fontSize = 22;
            classLabel.style.marginBottom = 2;
            Root.Add(classLabel);

            if (_tooltipService != null && def.PassiveDescriptions is { Length: > 0 })
            {
                var tooltipText = string.Join("\n", def.PassiveDescriptions);
                _tooltipService.Attach(classLabel, tooltipText);
            }
        }

        private VisualElement _statsAndStatusRow;

        private void BuildStatsAndStatusRow()
        {
            _statsAndStatusRow = new VisualElement();
            _statsAndStatusRow.style.flexDirection = FlexDirection.Row;
            _statsAndStatusRow.style.alignItems = Align.Center;
            Root.Add(_statsAndStatusRow);
            var row = _statsAndStatusRow;

            // Stats (left)
            var statsHeader = new Label("STATS:");
            statsHeader.style.color = Color.white;
            statsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsHeader.style.fontSize = 22;
            statsHeader.style.marginRight = 12;
            row.Add(statsHeader);

            _statLabels.Clear();
            int colCount = 4;
            int perCol = 2;
            for (int col = 0; col < colCount; col++)
            {
                var column = new VisualElement();
                column.style.flexDirection = FlexDirection.Column;
                column.style.marginRight = 20;
                row.Add(column);

                for (int r = 0; r < perCol; r++)
                {
                    int idx = col * perCol + r;
                    if (idx >= DisplayStats.Length) break;
                    var (statType, abbrev) = DisplayStats[idx];
                    int val = _entityStats.GetStat(_playerId, statType);
                    var label = new Label($"{abbrev}: {val}");
                    label.style.color = Color.white;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.fontSize = 22;
                    column.Add(label);
                    _statLabels[statType] = label;
                }
            }

            // Status effects (right, same row)
            _statusRow = new VisualElement();
            _statusRow.style.flexDirection = FlexDirection.Row;
            _statusRow.style.alignItems = Align.Center;
            _statusRow.style.marginLeft = 20;
            _statusRow.style.display = DisplayStyle.None;
            row.Add(_statusRow);

            var statusHeader = new Label("STATUS:");
            statusHeader.style.color = Color.white;
            statusHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusHeader.style.fontSize = 22;
            statusHeader.style.marginRight = 12;
            _statusRow.Add(statusHeader);

            _statusEffectsContainer = new VisualElement();
            _statusEffectsContainer.style.flexDirection = FlexDirection.Row;
            _statusEffectsContainer.style.alignItems = Align.FlexStart;
            _statusRow.Add(_statusEffectsContainer);
        }

        private void BuildGoldDisplay()
        {
            if (_resourceService == null || !_resourceService.Has(ResourceIds.Gold)) return;
            if (_statsAndStatusRow == null) return;

            int gold = (int)_resourceService.Get(ResourceIds.Gold);
            _goldLabel = new Label($"\U0001FA99 {gold}");
            _goldLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _goldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _goldLabel.style.fontSize = 22;
            _goldLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            _goldLabel.style.marginLeft = new StyleLength(StyleKeyword.Auto);
            _statsAndStatusRow.Add(_goldLabel);
        }

        private void SubscribeToEvents()
        {
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId)) UpdateHpBar();
            }));
            _subscriptions.Add(_eventBus.Subscribe<HealedEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId)) UpdateHpBar();
            }));
            _subscriptions.Add(_eventBus.Subscribe<ManaChangedEvent>(evt =>
            {
                if (!evt.EntityId.Equals(_playerId)) return;
                if (_isPreviewingManaCost)
                    ClearManaCostPreview();
                else
                    UpdateManaBar();
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatModifierAddedEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId)) UpdateStats();
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatModifierRemovedEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId)) UpdateStats();
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectAppliedEvent>(evt =>
            {
                if (evt.Target.Equals(_playerId)) UpdateStatusEffects();
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectRemovedEvent>(evt =>
            {
                if (evt.Target.Equals(_playerId)) UpdateStatusEffects();
            }));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectExpiredEvent>(evt =>
            {
                if (evt.Target.Equals(_playerId)) UpdateStatusEffects();
            }));
            _subscriptions.Add(_eventBus.Subscribe<ResourceChangedEvent>(evt =>
            {
                if (evt.Id != ResourceIds.Gold || _goldLabel == null) return;
                _goldLabel.text = $"\U0001FA99 {(int)evt.NewValue}";
            }));
        }

        private void UpdateStats()
        {
            foreach (var (statType, abbrev) in DisplayStats)
            {
                if (!_statLabels.TryGetValue(statType, out var label)) continue;
                int current = _entityStats.GetStat(_playerId, statType);
                int baseVal = _entityStats.GetBaseStat(_playerId, statType);
                label.text = $"{abbrev}: {current}";
                label.style.color = current > baseVal ? Color.green
                    : current < baseVal ? Color.red
                    : Color.white;
            }
        }

        private void UpdateStatusEffects()
        {
            if (_statusEffectsContainer == null || _statusRow == null) return;

            foreach (var h in _statusTooltipHandles) h.Dispose();
            _statusTooltipHandles.Clear();
            _statusEffectsContainer.Clear();

            var effects = _statusEffects.GetEffects(_playerId);
            if (effects == null || effects.Count == 0)
            {
                _statusRow.style.display = DisplayStyle.None;
                return;
            }

            _statusRow.style.display = DisplayStyle.Flex;

            int perCol = 2;
            VisualElement col = null;
            for (int i = 0; i < effects.Count; i++)
            {
                if (i % perCol == 0)
                {
                    col = new VisualElement();
                    col.style.flexDirection = FlexDirection.Column;
                    col.style.marginRight = 20;
                    _statusEffectsContainer.Add(col);
                }

                var eff = effects[i];
                var def = StatusEffectDefinitions.Get(eff.Type);
                var displayText = eff.StackCount > 1
                    ? $"{def.DisplayName} {eff.StackCount}"
                    : !eff.IsPermanent
                        ? $"{def.DisplayName} {eff.RemainingDuration}"
                        : def.DisplayName;
                var label = new Label(displayText);
                label.style.fontSize = 22;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.color = def.DisplayColor;
                col.Add(label);

                if (_tooltipService != null)
                {
                    var instance = eff;
                    var content = TooltipContent.FromCustom(() =>
                        TooltipContentBuilder.BuildStatusEffectTooltipContent(instance));
                    var handle = _tooltipService.RegisterHover(
                        label, content, TooltipPlacement.Bottom, TooltipStyles.EntityTooltip);
                    _statusTooltipHandles.Add(handle);
                }
            }
        }
    }
}
