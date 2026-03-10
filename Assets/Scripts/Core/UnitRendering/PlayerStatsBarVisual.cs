using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using Unidad.Core.EventBus;
using Unidad.Core.UI.Components;
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
        private readonly EntityId _playerId;
        private readonly Dictionary<StatType, Label> _statLabels = new();
        private readonly List<IDisposable> _subscriptions = new();

        private VisualElement _statusRow;
        private VisualElement _statusEffectsContainer;

        public VisualElement Root { get; private set; }
        public UnidadProgressBar HpBar { get; private set; }
        public Label HpLabel { get; private set; }
        public UnidadProgressBar ManaBar { get; private set; }
        public Label ManaLabel { get; private set; }
        public VisualElement ManaCostOverlay { get; private set; }

        public PlayerStatsBarVisual(IEventBus eventBus, IEntityStatsService entityStats,
            IStatusEffectService statusEffects, EntityId playerId)
        {
            _eventBus = eventBus;
            _entityStats = entityStats;
            _statusEffects = statusEffects;
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
            Root.style.paddingTop = 8;
            Root.style.paddingBottom = 8;

            BuildHpBar();
            BuildManaBar();
            BuildStatsGrid();
            BuildStatusEffects();
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

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _statLabels.Clear();
            _statusEffectsContainer = null;
            _statusRow = null;
            Root = null;
            HpBar = null;
            HpLabel = null;
            ManaBar = null;
            ManaLabel = null;
            ManaCostOverlay = null;
        }

        private void BuildHpBar()
        {
            var hpBarWrapper = new VisualElement();
            hpBarWrapper.style.width = Length.Percent(100);
            hpBarWrapper.style.height = 48;
            hpBarWrapper.style.marginBottom = 8;
            hpBarWrapper.style.justifyContent = Justify.Center;

            HpBar = new UnidadProgressBar(1f);
            HpBar.SetVariant(ProgressVariant.Success);
            HpBar.style.width = Length.Percent(100);
            HpBar.style.height = 16;
            hpBarWrapper.Add(HpBar);

            int hp = _entityStats.GetCurrentHealth(_playerId);
            int maxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            HpLabel = new Label($"{hp}/{maxHp}");
            HpLabel.style.position = Position.Absolute;
            HpLabel.style.top = 0;
            HpLabel.style.left = 0;
            HpLabel.style.right = 0;
            HpLabel.style.bottom = 0;
            HpLabel.style.fontSize = 36;
            HpLabel.style.color = Color.white;
            HpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            HpLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hpBarWrapper.Add(HpLabel);

            Root.Add(hpBarWrapper);
        }

        private void BuildManaBar()
        {
            var manaBarWrapper = new VisualElement();
            manaBarWrapper.style.width = Length.Percent(100);
            manaBarWrapper.style.height = 48;
            manaBarWrapper.style.marginBottom = 8;
            manaBarWrapper.style.justifyContent = Justify.Center;

            ManaBar = new UnidadProgressBar(0.5f);
            ManaBar.SetVariant(ProgressVariant.Info);
            ManaBar.style.width = Length.Percent(100);
            ManaBar.style.height = 16;
            manaBarWrapper.Add(ManaBar);

            ManaCostOverlay = new VisualElement();
            ManaCostOverlay.style.position = Position.Absolute;
            ManaCostOverlay.style.top = 0;
            ManaCostOverlay.style.bottom = 0;
            ManaCostOverlay.style.display = DisplayStyle.None;
            ManaCostOverlay.pickingMode = PickingMode.Ignore;
            ManaBar.Add(ManaCostOverlay);

            int mana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            ManaLabel = new Label($"{mana}/{maxMana}");
            ManaLabel.style.position = Position.Absolute;
            ManaLabel.style.top = 0;
            ManaLabel.style.left = 0;
            ManaLabel.style.right = 0;
            ManaLabel.style.bottom = 0;
            ManaLabel.style.fontSize = 36;
            ManaLabel.style.color = Color.white;
            ManaLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ManaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            manaBarWrapper.Add(ManaLabel);

            Root.Add(manaBarWrapper);
        }

        private void BuildStatsGrid()
        {
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.alignItems = Align.Center;
            Root.Add(statsRow);

            var statsHeader = new Label("STATS:");
            statsHeader.style.color = Color.white;
            statsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsHeader.style.fontSize = 27;
            statsHeader.style.marginRight = 12;
            statsRow.Add(statsHeader);

            _statLabels.Clear();
            int colCount = 4;
            int perCol = 2;
            for (int col = 0; col < colCount; col++)
            {
                var column = new VisualElement();
                column.style.flexDirection = FlexDirection.Column;
                column.style.marginRight = 20;
                statsRow.Add(column);

                for (int row = 0; row < perCol; row++)
                {
                    int idx = col * perCol + row;
                    if (idx >= DisplayStats.Length) break;
                    var (statType, abbrev) = DisplayStats[idx];
                    int val = _entityStats.GetStat(_playerId, statType);
                    var label = new Label($"{abbrev}: {val}");
                    label.style.color = Color.white;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.fontSize = 27;
                    column.Add(label);
                    _statLabels[statType] = label;
                }
            }
        }

        private void BuildStatusEffects()
        {
            _statusRow = new VisualElement();
            _statusRow.style.flexDirection = FlexDirection.Row;
            _statusRow.style.alignItems = Align.Center;
            _statusRow.style.marginTop = 4;
            _statusRow.style.display = DisplayStyle.None;
            Root.Add(_statusRow);

            var statusHeader = new Label("STATUS:");
            statusHeader.style.color = Color.white;
            statusHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusHeader.style.fontSize = 27;
            statusHeader.style.marginRight = 12;
            _statusRow.Add(statusHeader);

            _statusEffectsContainer = new VisualElement();
            _statusEffectsContainer.style.flexDirection = FlexDirection.Row;
            _statusEffectsContainer.style.alignItems = Align.FlexStart;
            _statusRow.Add(_statusEffectsContainer);
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
                if (evt.EntityId.Equals(_playerId)) UpdateManaBar();
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
                string text = eff.StackCount > 1 ? $"{def.DisplayName}({eff.StackCount})" : def.DisplayName;
                var label = new Label(text);
                label.style.fontSize = 27;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.color = def.DisplayColor;
                col.Add(label);
            }
        }
    }
}
