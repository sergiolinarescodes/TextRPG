using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Consumable;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using Unidad.Core.EventBus;
using Unidad.Core.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.UnitRendering
{
    internal sealed class WordInputController : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IWordInputService _wordInputService;
        private readonly DrunkLetterService _drunkLetterService;
        private readonly IWordMatchService _wordMatchService;
        private readonly IWordMatchService _ammoMatchService;
        private readonly IWordResolver _wordResolver;
        private readonly ICombatContext _combatContext;
        private readonly ITargetingPreviewService _previewService;
        private readonly ITargetingPreviewService _ammoPreviewService;
        private readonly IWeaponService _weaponService;
        private readonly IConsumableService _consumableService;
        private readonly PlayerStatsBarVisual _statsBar;
        private readonly CombatSlotVisual _slotVisual;
        private readonly EntityId _playerId;
        private readonly float _fontScaleFactor;
        private readonly List<IDisposable> _subscriptions = new();

        private ICombatLoopService _combatLoop;
        private IEventEncounterLoopService _eventLoop;
        private string _lastMatchedWord = "";
        private bool _givePrefixDetected;
        private IVisualElementScheduledItem _drunkWobbleSchedule;

        public AnimatedCodeField CodeField { get; private set; }
        public VisualElement MainTextPanel { get; private set; }
        private VisualElement _linesContainer;

        public WordInputController(IEventBus eventBus, IWordInputService wordInputService,
            DrunkLetterService drunkLetterService, IWordMatchService wordMatchService,
            IWordMatchService ammoMatchService, IWordResolver wordResolver,
            ICombatContext combatContext, ITargetingPreviewService previewService,
            ITargetingPreviewService ammoPreviewService, IWeaponService weaponService,
            IConsumableService consumableService, PlayerStatsBarVisual statsBar,
            CombatSlotVisual slotVisual, EntityId playerId, float fontScaleFactor)
        {
            _eventBus = eventBus;
            _wordInputService = wordInputService;
            _drunkLetterService = drunkLetterService;
            _wordMatchService = wordMatchService;
            _ammoMatchService = ammoMatchService;
            _wordResolver = wordResolver;
            _combatContext = combatContext;
            _previewService = previewService;
            _ammoPreviewService = ammoPreviewService;
            _weaponService = weaponService;
            _consumableService = consumableService;
            _statsBar = statsBar;
            _slotVisual = slotVisual;
            _playerId = playerId;
            _fontScaleFactor = fontScaleFactor;

            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnStartedEvent>(_ => SetInputEnabled(true)));
            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnEndedEvent>(_ => SetInputEnabled(false)));
            _subscriptions.Add(_eventBus.Subscribe<DrunkLettersChangedEvent>(_ => RefreshDrunkVisuals()));
        }

        public void SetCombatLoop(ICombatLoopService combatLoop) => _combatLoop = combatLoop;
        public void SetEventLoop(IEventEncounterLoopService eventLoop) => _eventLoop = eventLoop;

        public VisualElement BuildInputArea(float vibrationAmplitude)
        {
            MainTextPanel = new VisualElement();
            MainTextPanel.style.flexGrow = 1;
            MainTextPanel.style.backgroundColor = Color.black;
            MainTextPanel.style.justifyContent = Justify.Center;
            MainTextPanel.style.alignItems = Align.Stretch;
            MainTextPanel.style.overflow = Overflow.Hidden;

            CodeField = new AnimatedCodeField();
            CodeField.multiline = false;
            CodeField.PersistentFocus = true;
            CodeField.TypingAnimationAmplitude = vibrationAmplitude;
            CodeField.style.width = Length.Percent(100);
            CodeField.style.flexGrow = 1;
            CodeField.style.paddingTop = 0;
            CodeField.style.paddingBottom = 0;
            CodeField.style.paddingLeft = 0;
            CodeField.style.paddingRight = 0;
            CodeField.style.marginTop = 0;
            CodeField.style.marginBottom = 0;
            CodeField.style.marginLeft = 0;
            CodeField.style.marginRight = 0;
            CodeField.style.color = Color.white;
            MainTextPanel.Add(CodeField);

            _linesContainer = CodeField.Q(className: "animated-code-field__lines");
            if (_linesContainer != null)
            {
                _linesContainer.style.paddingLeft = 0;
                _linesContainer.style.paddingTop = 0;
                _linesContainer.style.paddingRight = 0;
                _linesContainer.style.paddingBottom = 0;
                _linesContainer.style.justifyContent = Justify.Center;
            }

            CodeField.RegisterValueChangedCallback(OnTextChanged);
            CodeField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            MainTextPanel.RegisterCallback<GeometryChangedEvent>(_ => RecalculateFontSize());

            CodeField.schedule.Execute(() => CodeField?.Focus());

            return MainTextPanel;
        }

        public void SetInputEnabled(bool enabled)
        {
            if (CodeField == null) return;
            CodeField.PersistentFocus = enabled;
            CodeField.SetEnabled(enabled);
            if (enabled)
                CodeField.schedule.Execute(() => CodeField?.Focus());
        }

        private bool IsAmmoWord(string word) =>
            (_weaponService != null && _weaponService.HasWeapon(_playerId)
             && _weaponService.IsAmmoForEquipped(_playerId, word))
            || (_consumableService != null && _consumableService.HasConsumable(_playerId)
                && _consumableService.IsAmmoForEquipped(_playerId, word));

        public void FireWeapon()
        {
            if (_combatLoop == null || !_combatLoop.FireWeapon()) return;
            ClearAfterSubmit();
        }

        public void UseConsumable()
        {
            if (_combatLoop == null || !_combatLoop.UseConsumable()) return;
            ClearAfterSubmit();
        }

        private void ClearAfterSubmit()
        {
            _wordInputService.Clear();
            CodeField.value = "";
            ClearTargetingPreview();
            _statsBar.ClearManaCostPreview();
            CodeField?.schedule.Execute(() => CodeField?.Focus());
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            var newText = evt.newValue ?? "";

            _wordInputService.Clear();
            foreach (var c in newText)
                _wordInputService.AppendCharacter(c);

            var displayText = _wordInputService.CurrentWord;

            RecalculateFontSize();

            // Update label text to show remapped characters immediately
            if (_drunkLetterService != null && _drunkLetterService.IsActive)
            {
                var labels = CodeField.CharLabels;
                for (int i = 0; i < labels.Count && i < displayText.Length; i++)
                    labels[i].text = displayText[i].ToString();
            }

            // Detect and apply "give " prefix
            var (isGivePrefix, matchWord, prefixLen) = GivePrefixVisual.ApplyGivePrefix(
                displayText, CodeField, ref _givePrefixDetected);
            var lowerText = displayText.ToLowerInvariant();

            bool isAmmo = IsAmmoWord(matchWord);
            var matchService = isAmmo ? _ammoMatchService : _wordMatchService;
            var wasMatched = matchService.IsMatched;
            var colors = matchService.CheckMatch(matchWord);

            if (colors.Count > 0)
            {
                var labels = CodeField.CharLabels;
                for (int i = 0; i < colors.Count && i + prefixLen < labels.Count; i++)
                    labels[i + prefixLen].style.color = colors[i].Color;

                if (!wasMatched)
                {
                    var indices = new List<int>();
                    for (int i = 0; i < colors.Count; i++)
                        indices.Add(i + prefixLen);
                    CodeField.PlayHighlightAnimation(indices);
                }

                var currentWord = lowerText;
                if (currentWord != _lastMatchedWord)
                {
                    _lastMatchedWord = currentWord;
                    ShowTargetingPreview(displayText);
                    if (!isAmmo)
                        _statsBar.ShowManaCostPreview(_wordResolver, matchWord.ToLowerInvariant());
                    else
                        _statsBar.ClearManaCostPreview();
                }
            }
            else
            {
                var labels = CodeField.CharLabels;
                for (int i = prefixLen; i < labels.Count; i++)
                    labels[i].style.color = Color.white;

                if (!isGivePrefix)
                {
                    for (int i = 0; i < labels.Count; i++)
                        labels[i].style.color = Color.white;
                }

                _lastMatchedWord = "";
                ClearTargetingPreview();
                _statsBar.ClearManaCostPreview();
            }

            // Apply drunk visual override
            if (_drunkLetterService != null && _drunkLetterService.IsActive)
            {
                var drunkLabels = CodeField.CharLabels;
                for (int i = 0; i < drunkLabels.Count && i < newText.Length; i++)
                {
                    var original = char.ToLowerInvariant(newText[i]);
                    if (_drunkLetterService.IsLetterDrunk(original))
                    {
                        drunkLabels[i].style.color = new Color(1f, 0.85f, 0.2f);
                        drunkLabels[i].AddToClassList("drunk-letter");
                    }
                    else
                    {
                        drunkLabels[i].RemoveFromClassList("drunk-letter");
                    }
                }
                UpdateDrunkWobble();
            }
        }

        private void ShowTargetingPreview(string text)
        {
            TargetingPreviewVisual.ShowPreview(text, _slotVisual, _combatContext,
                _previewService, _ammoPreviewService, IsAmmoWord);
        }

        private void ClearTargetingPreview()
        {
            TargetingPreviewVisual.ClearPreview(_slotVisual);
        }

        private void SubmitCurrentWord()
        {
            var word = _wordInputService.CurrentWord?.Trim() ?? "";
            if (word.Length == 0) return;

            WordSubmitResult result;
            if (_combatLoop != null)
                result = _combatLoop.SubmitWord(word);
            else if (_eventLoop != null)
                result = _eventLoop.SubmitWord(word);
            else
                return;

            if (result == WordSubmitResult.InsufficientMana || result == WordSubmitResult.WordOnCooldown || result == WordSubmitResult.NoItemToGive)
            {
                PlayManaRejection();
                CodeField?.schedule.Execute(() => CodeField?.Focus());
                return;
            }
            if (result == WordSubmitResult.ReservedWord)
            {
                ClearAfterSubmit();
                return;
            }
            if (result != WordSubmitResult.Accepted) return;

            ClearAfterSubmit();
        }

        private void PlayManaRejection()
        {
            var labels = CodeField.CharLabels;
            var indices = new List<int>();
            for (int i = 0; i < labels.Count; i++)
                indices.Add(i);
            CodeField.PlayRejectionAnimation(indices);
            _statsBar.FlashManaDanger();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_combatLoop != null && !_combatLoop.IsPlayerTurn) return;
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCurrentWord();
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private void RecalculateFontSize()
        {
            if (CodeField == null || MainTextPanel == null) return;

            var widthSource = _linesContainer ?? (VisualElement)MainTextPanel;
            var panelWidth = widthSource.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0) return;

            var text = CodeField.value ?? "";
            var charCount = Mathf.Max(text.Length, 1);

            var ratio = CodeField.BaseCharWidthRatio;
            var fontSize = panelWidth / (charCount * ratio);
            fontSize = Mathf.Clamp(fontSize, 12f, 800f);
            CodeField.SetCharFontSize(fontSize);

            if (charCount > 0 && text.Length > 0)
            {
                var labels = CodeField.CharLabels;
                if (labels.Count == 0) return;

                var localFontSize = fontSize;
                var localPanelWidth = panelWidth;
                EventCallback<GeometryChangedEvent> correctionCallback = null;
                correctionCallback = _ =>
                {
                    labels[0].UnregisterCallback(correctionCallback);
                    if (CodeField == null || MainTextPanel == null) return;

                    var currentLabels = CodeField.CharLabels;
                    if (currentLabels.Count == 0) return;

                    float totalWidth = 0;
                    foreach (var label in currentLabels)
                    {
                        var w = label.resolvedStyle.width;
                        if (!float.IsNaN(w) && w > 0) totalWidth += w;
                    }

                    if (totalWidth <= 0) return;

                    var targetWidth = localPanelWidth * _fontScaleFactor;
                    var correction = targetWidth / totalWidth;
                    var correctedSize = localFontSize * correction;
                    correctedSize = Mathf.Clamp(correctedSize, 12f, 800f);
                    CodeField.SetCharFontSize(correctedSize);
                };
                labels[0].RegisterCallback(correctionCallback);
            }
        }

        private void RefreshDrunkVisuals()
        {
            if (CodeField == null) return;
            var labels = CodeField.CharLabels;
            if (labels.Count == 0) return;

            var originalText = CodeField.value ?? "";
            for (int i = 0; i < labels.Count && i < originalText.Length; i++)
            {
                var original = char.ToLowerInvariant(originalText[i]);
                if (_drunkLetterService != null && _drunkLetterService.IsLetterDrunk(original))
                {
                    var remapped = _drunkLetterService.RemapInput(original);
                    labels[i].text = remapped.ToString();
                    labels[i].style.color = new Color(1f, 0.85f, 0.2f);
                    labels[i].AddToClassList("drunk-letter");
                }
                else
                {
                    labels[i].text = original.ToString();
                    labels[i].RemoveFromClassList("drunk-letter");
                }
            }

            UpdateDrunkWobble();
        }

        private void UpdateDrunkWobble()
        {
            if (_drunkLetterService == null || !_drunkLetterService.IsActive)
            {
                _drunkWobbleSchedule?.Pause();
                _drunkWobbleSchedule = null;
                if (CodeField != null)
                {
                    var labels = CodeField.CharLabels;
                    for (int i = 0; i < labels.Count; i++)
                    {
                        labels[i].style.translate = new Translate(0, 0);
                        labels[i].RemoveFromClassList("drunk-letter");
                    }
                }
                return;
            }

            if (_drunkWobbleSchedule != null) return;

            _drunkWobbleSchedule = CodeField.schedule.Execute(() =>
            {
                if (CodeField == null || _drunkLetterService == null) return;
                var labels = CodeField.CharLabels;
                var text = CodeField.value ?? "";
                float time = Time.time;
                for (int i = 0; i < labels.Count && i < text.Length; i++)
                {
                    if (!labels[i].ClassListContains("drunk-letter")) continue;
                    float offsetX = Mathf.Sin(time * 8 + i) * 3f;
                    float offsetY = Mathf.Cos(time * 6 + i * 0.5f) * 2f;
                    labels[i].style.translate = new Translate(offsetX, offsetY);
                }
            }).Every(16);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _drunkWobbleSchedule?.Pause();
            _drunkWobbleSchedule = null;
            _combatLoop = null;
            _eventLoop = null;
            CodeField = null;
            MainTextPanel = null;
            _linesContainer = null;
        }
    }
}
