using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.Encounter;
using TextRPG.Core.EnemyAI;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Grid;
using Unidad.Core.Testing;
using Unidad.Core.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

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
        private IUnitService _unitService;
        private AnimatedCodeField _codeField;
        private VisualElement _mainTextPanel;
        private VisualElement _statsBar;
        private VisualElement _tileMapPanel;
        private int _gridWidth;
        private int _gridHeight;
        private float _fontScaleFactor;
        private VisualElement _linesContainer;
        private readonly List<IDisposable> _subscriptions = new();
        private UnitGridVisual _gridVisual;
        private IWordMatchService _wordMatchService;
        private IWordResolver _wordResolver;
        private ITargetingPreviewService _previewService;
        private ITargetingPreviewService _ammoPreviewService;
        private ICombatContext _combatContext;
        private ICombatGridService _combatGrid;
        private IEntityStatsService _entityStats;
        private VisualElement _clickOverlay;

        private IWeaponService _weaponService;
        private IWeaponActionExecutor _weaponExecutor;
        private IActionExecutionService _actionExecution;
        private IWordResolver _ammoResolver;
        private IWordMatchService _ammoMatchService;
        private VisualElement _weaponSlot;
        private Label _weaponDurabilityLabel;
        private VisualElement _weaponNameContainer;
        private bool _isWeaponMode;
        private EntityId _playerId;
        private readonly List<(VisualElement cell, EventCallback<ClickEvent> cb)> _cellClickHandlers = new();
        private string _lastMatchedWord = "";

        private ITurnService _turnService;
        private IEnemyAIService _enemyAI;
        private ScenarioEncounterAdapter _encounterAdapter;
        private bool _isPlayerTurn;
        private bool _gameOver;
        private UnidadProgressBar _hpBar;
        private Label _hpLabel;

        private static readonly Color HighlightEnemy = new(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HighlightSelf = new(0.3f, 1f, 0.3f, 0.4f);
        private static readonly Color WeaponSlotBorderDefault = new(0.5f, 0.5f, 0.5f);
        private static readonly Color WeaponSlotBorderActive = new(1f, 0.8f, 0.2f);

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
            _eventBus = new EventBus();
            _service = new WordInputService(_eventBus);
            _unitService = new UnitService(_eventBus);
            _entityStats = new EntityStatsService(_eventBus);
            var wordActionData = WordActionDatabaseLoader.Load();
            _wordResolver = new FilteredWordResolver(wordActionData.Resolver, wordActionData.AmmoWordSet);
            _ammoResolver = wordActionData.AmmoResolver;
            _wordMatchService = new WordMatchService(_wordResolver, wordActionData.ActionRegistry);
            _ammoMatchService = new WordMatchService(_ammoResolver, wordActionData.ActionRegistry);
            // CombatGrid + CombatContext
            _combatGrid = new CombatGridService(_eventBus, _unitService);
            _combatGrid.Initialize(_gridWidth, _gridHeight);

            _combatContext = new CombatContext();
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetGrid(_combatGrid);

            // Turn system
            _turnService = new TurnService(_eventBus);

            // StatusEffect (needed for scorer registry and full handlers)
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            var statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            // Weapon service + action execution
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordActionData);
            _weaponService = new WeaponService(_eventBus, weaponRegistry);

            // Full action handler registry (Damage, Heal, Burn, Shock, Fear, Stun, Weapon, etc.)
            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, _combatContext,
                statusEffects, _turnService, _weaponService);
            var handlerRegistry = ActionHandlerRegistryFactory.CreateDefault(actionHandlerCtx);

            // Place player unit at center-bottom
            _playerId = new EntityId("player");
            var playerPos = new GridPosition(_gridWidth / 2, 0);
            var playerDef = new UnitDefinition(
                new UnitId("player"), "YOU", 100, 10, 8, 12, Color.white);
            _entityStats.RegisterEntity(_playerId, 100, 10, 8, 5, 4, 3);
            _combatGrid.RegisterCombatant(_playerId, playerDef, playerPos);

            // Place enemy units
            var enemyIds = new List<EntityId>();
            var enemyDefs = new[]
            {
                ("goblin", "GOBLIN", new GridPosition(0, _gridHeight - 1), new Color(0.2f, 0.8f, 0.2f)),
                ("skeleton", "SKELETON", new GridPosition(_gridWidth / 2, _gridHeight - 1), new Color(0.9f, 0.9f, 0.8f)),
                ("bat", "BAT", new GridPosition(Mathf.Min(_gridWidth - 1, 2), _gridHeight - 2), new Color(0.6f, 0.3f, 0.8f)),
            };

            foreach (var (id, name, pos, color) in enemyDefs)
            {
                if (!_combatGrid.Grid.IsInBounds(pos)) continue;
                var entityId = new EntityId(id);
                var unitDef = new UnitDefinition(new UnitId(id), name, 50, 6, 5, 4, color);
                _entityStats.RegisterEntity(entityId, 50, 6, 4, 3, 2, 2);
                _combatGrid.RegisterCombatant(entityId, unitDef, pos);
                enemyIds.Add(entityId);
            }

            _combatContext.SetSourceEntity(_playerId);
            _combatContext.SetEnemies(enemyIds.ToArray());
            _combatContext.SetAllies(Array.Empty<EntityId>());

            _previewService = new TargetingPreviewService(_wordResolver, _combatContext);
            _ammoPreviewService = new TargetingPreviewService(_ammoResolver, _combatContext);

            // --- Encounter adapter + Enemy AI ---
            _encounterAdapter = new ScenarioEncounterAdapter();
            var enemyResolver = new EnemyWordResolver();

            var enemyAbilities = new (string id, EnemyDefinition def, (string word, string target, int damage)[] words)[]
            {
                ("goblin", new EnemyDefinition("GOBLIN", 50, 6, 0, 4, 2, 2, 2,
                    new Color(0.2f, 0.8f, 0.2f), new[] { "scratch", "hit" }),
                    new[] { ("scratch", "Melee", 1), ("hit", "Melee", 2) }),

                ("skeleton", new EnemyDefinition("SKELETON", 50, 8, 0, 5, 3, 2, 2,
                    new Color(0.9f, 0.9f, 0.8f), new[] { "slash", "strike" }),
                    new[] { ("slash", "Melee", 3), ("strike", "Melee", 3) }),

                ("bat", new EnemyDefinition("BAT", 30, 4, 0, 3, 2, 3, 3,
                    new Color(0.6f, 0.3f, 0.8f), new[] { "scratch" }),
                    new[] { ("scratch", "Melee", 1) }),
            };

            for (int i = 0; i < enemyIds.Count && i < enemyAbilities.Length; i++)
            {
                _encounterAdapter.RegisterEnemy(enemyIds[i], enemyAbilities[i].def);
                foreach (var (word, target, damage) in enemyAbilities[i].words)
                {
                    if (!enemyResolver.HasWord(word))
                    {
                        enemyResolver.RegisterWord(word,
                            new List<WordActionMapping> { new("Damage", damage) },
                            new WordMeta(target, 0, target == "Melee" ? 1 : 3));
                    }
                }
            }
            _encounterAdapter.Activate();

            // Composite resolver for AI action execution
            var compositeResolver = new CompositeWordResolver(_wordResolver, enemyResolver);
            _actionExecution = new ActionExecutionService(_eventBus, compositeResolver, handlerRegistry, _combatContext);

            // Weapon executor — processes WeaponAmmoSubmittedEvent → runs ammo actions
            _weaponExecutor = new WeaponActionExecutor(
                _eventBus, _weaponService, _ammoResolver, handlerRegistry, _combatContext);

            // Scorer registry (Unidad IContributor pattern)
            var scorers = EnemyAISystemInstaller.CreateScorerRegistry(statusEffects);

            // EnemyAI service (real AI with scoring)
            _enemyAI = new EnemyAIService(_eventBus, _encounterAdapter, _entityStats,
                _turnService, _combatGrid, _combatContext, _actionExecution, scorers, enemyResolver);

            // Turn order: player first, then enemies
            var turnOrder = new List<EntityId> { _playerId };
            turnOrder.AddRange(enemyIds);
            _turnService.SetTurnOrder(turnOrder);
            _turnService.BeginTurn();
            _isPlayerTurn = true;
            Debug.Log("[Turn] === Player's turn (Turn #1, Round #1) ===");

            // --- Subscribe to weapon events ---
            _subscriptions.Add(_eventBus.Subscribe<WeaponEquippedEvent>(OnWeaponEquipped));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDurabilityChangedEvent>(OnWeaponDurabilityChanged));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDestroyedEvent>(OnWeaponDestroyed));

            // --- Subscribe to events ---
            _subscriptions.Add(_eventBus.Subscribe<WordSubmittedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var actions = _wordResolver.Resolve(word);
                var meta = _wordResolver.GetStats(word);
                if (actions.Count > 0)
                {
                    var actionList = string.Join(", ", actions.Select(a => $"{a.ActionId}({a.Value})"));
                    Debug.Log($"[WordInputScenario] \"{evt.Word}\" → {meta.Target} cost={meta.Cost} range={meta.Range} area={meta.Area} | {actionList}");
                }
                else
                {
                    Debug.Log($"[WordInputScenario] \"{evt.Word}\" → no actions");
                }
            }));

            _subscriptions.Add(_eventBus.Subscribe<WordClearedEvent>(_ =>
            {
                Debug.Log("[WordInputScenario] Word cleared");
            }));

            _subscriptions.Add(_eventBus.Subscribe<GridCellChangedEvent>(evt =>
            {
                _gridVisual?.UpdateCell(evt.Position);
            }));

            // Re-render grid cells when HP changes + update player HP bar
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                RefreshEntityCell(evt.EntityId);
                if (evt.EntityId.Equals(_playerId))
                    UpdatePlayerHpBar();
            }));
            _subscriptions.Add(_eventBus.Subscribe<HealedEvent>(evt =>
            {
                RefreshEntityCell(evt.EntityId);
                if (evt.EntityId.Equals(_playerId))
                    UpdatePlayerHpBar();
            }));
            _subscriptions.Add(_eventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                _encounterAdapter.MarkDead(evt.EntityId);
                RefreshEntityCell(evt.EntityId);
                if (evt.EntityId.Equals(_playerId))
                    OnPlayerDied();
            }));

            // Log enemy actions
            _subscriptions.Add(_eventBus.Subscribe<ActionHandlerExecutedEvent>(evt =>
            {
                if (!_isPlayerTurn)
                    Debug.Log($"[Turn:Enemy] {evt.ActionId}({evt.Value}) → {evt.Targets.Count} target(s)");
            }));
            _subscriptions.Add(_eventBus.Subscribe<CombatantMovedEvent>(evt =>
            {
                if (!_isPlayerTurn)
                    Debug.Log($"[Turn:Enemy] {evt.EntityId.Value} moved ({evt.From.X},{evt.From.Y}) → ({evt.To.X},{evt.To.Y})");
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
            _statsBar.style.flexGrow = 0;
            _statsBar.style.flexShrink = 0;
            _statsBar.style.height = 100;
            _statsBar.style.backgroundColor = Color.black;
            _statsBar.style.flexDirection = FlexDirection.Column;
            _statsBar.style.justifyContent = Justify.Center;
            _statsBar.style.paddingLeft = 10;
            _statsBar.style.paddingRight = 10;
            leftColumn.Add(_statsBar);

            // HP bar (full width) with label centered vertically
            var hpBarWrapper = new VisualElement();
            hpBarWrapper.style.width = Length.Percent(100);
            hpBarWrapper.style.height = 48;
            hpBarWrapper.style.marginBottom = 8;
            hpBarWrapper.style.justifyContent = Justify.Center;

            _hpBar = new UnidadProgressBar(1f);
            _hpBar.SetVariant(ProgressVariant.Success);
            _hpBar.style.width = Length.Percent(100);
            _hpBar.style.height = 16;
            hpBarWrapper.Add(_hpBar);

            int playerHp = _entityStats.GetCurrentHealth(_playerId);
            int playerMaxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            _hpLabel = new Label($"{playerHp}/{playerMaxHp}");
            _hpLabel.style.position = Position.Absolute;
            _hpLabel.style.top = 0;
            _hpLabel.style.left = 0;
            _hpLabel.style.right = 0;
            _hpLabel.style.bottom = 0;
            _hpLabel.style.fontSize = 36;
            _hpLabel.style.color = Color.white;
            _hpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _hpLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hpBarWrapper.Add(_hpLabel);

            _statsBar.Add(hpBarWrapper);

            // Stats row
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.alignItems = Align.Center;
            _statsBar.Add(statsRow);

            var statsLabel = new Label("STR: 10  DEX: 8  INT: 12");
            statsLabel.style.color = Color.white;
            statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsLabel.style.fontSize = 38;
            statsLabel.style.marginRight = 20;
            statsRow.Add(statsLabel);

            var statusLabel = new Label("Status: Normal");
            statusLabel.style.color = Color.white;
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusLabel.style.fontSize = 38;
            statsRow.Add(statusLabel);

            // --- Right Column: Tile Map ---
            _tileMapPanel = new VisualElement();
            _tileMapPanel.style.flexGrow = 0;
            _tileMapPanel.style.flexShrink = 0;
            _tileMapPanel.style.width = 240;
            _tileMapPanel.style.backgroundColor = Color.black;
            _tileMapPanel.style.flexDirection = FlexDirection.ColumnReverse;
            _tileMapPanel.style.paddingTop = 20;
            _tileMapPanel.style.paddingBottom = 20;
            _tileMapPanel.style.paddingLeft = 4;
            _tileMapPanel.style.paddingRight = 4;
            root.Add(_tileMapPanel);

            _gridVisual = new UnitGridVisual(_combatGrid.Grid, _unitService, _entityStats, _gridWidth, _gridHeight,
                new HashSet<string> { "player" });
            _tileMapPanel.RegisterCallback<GeometryChangedEvent>(_ => _gridVisual?.RefreshFontSizes());
            _gridVisual.Build(_tileMapPanel);

            // Weapon slot (bottom-right of main text panel, initially hidden)
            _weaponSlot = new VisualElement();
            _weaponSlot.style.position = Position.Absolute;
            _weaponSlot.style.bottom = 110;
            _weaponSlot.style.right = 10;
            _weaponSlot.style.width = 80;
            _weaponSlot.style.height = 80;
            _weaponSlot.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _weaponSlot.style.borderTopWidth = 2;
            _weaponSlot.style.borderBottomWidth = 2;
            _weaponSlot.style.borderLeftWidth = 2;
            _weaponSlot.style.borderRightWidth = 2;
            _weaponSlot.style.borderTopColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderBottomColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderLeftColor = WeaponSlotBorderDefault;
            _weaponSlot.style.borderRightColor = WeaponSlotBorderDefault;
            _weaponSlot.style.flexDirection = FlexDirection.Column;
            _weaponSlot.style.justifyContent = Justify.Center;
            _weaponSlot.style.alignItems = Align.Center;
            _weaponSlot.style.overflow = Overflow.Hidden;
            _weaponSlot.style.display = DisplayStyle.None;
            _weaponSlot.pickingMode = PickingMode.Position;

            _weaponNameContainer = new VisualElement();
            _weaponNameContainer.style.flexDirection = FlexDirection.Column;
            _weaponNameContainer.style.justifyContent = Justify.Center;
            _weaponNameContainer.style.alignItems = Align.Center;
            _weaponNameContainer.style.flexGrow = 1;
            _weaponSlot.Add(_weaponNameContainer);

            _weaponDurabilityLabel = new Label();
            _weaponDurabilityLabel.style.color = new Color(1f, 0.8f, 0.2f);
            _weaponDurabilityLabel.style.fontSize = 20;
            _weaponDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _weaponDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _weaponDurabilityLabel.style.position = Position.Absolute;
            _weaponDurabilityLabel.style.bottom = 2;
            _weaponDurabilityLabel.style.left = 4;
            _weaponSlot.Add(_weaponDurabilityLabel);

            _weaponSlot.RegisterCallback<ClickEvent>(_ =>
            {
                _isWeaponMode = !_isWeaponMode;
                UpdateWeaponSlotBorder();
                Debug.Log($"[WordInputScenario] Weapon mode: {(_isWeaponMode ? "ON" : "OFF")}");

                if (_isWeaponMode)
                    ShowWeaponRangePreview();
                else
                    ClearTargetingPreview();

                _codeField?.schedule.Execute(() => _codeField?.Focus());
            });

            leftColumn.Add(_weaponSlot);

            // Focus the code field after a frame so it's attached to the panel
            _codeField.schedule.Execute(() => _codeField.Focus());
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            var newText = evt.newValue ?? "";

            // Sync service state
            _service.Clear();
            foreach (var c in newText)
                _service.AppendCharacter(c);

            RecalculateFontSize();

            // Word match detection — use ammo resolver in weapon mode
            var matchService = _isWeaponMode ? _ammoMatchService : _wordMatchService;
            var wasMatched = matchService.IsMatched;
            var colors = matchService.CheckMatch(newText);
            if (colors.Count > 0)
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < colors.Count && i < labels.Count; i++)
                    labels[i].style.color = colors[i].Color;

                if (!wasMatched)
                {
                    var indices = new List<int>();
                    for (int i = 0; i < colors.Count; i++)
                        indices.Add(i);
                    _codeField.PlayHighlightAnimation(indices);
                }

                // Preview targeting on grid (skip if word hasn't changed)
                var currentWord = newText.ToLowerInvariant();
                if (currentWord != _lastMatchedWord)
                {
                    _lastMatchedWord = currentWord;
                    ShowTargetingPreview(newText);
                    CreateClickOverlay(newText);
                }
            }
            else
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < labels.Count; i++)
                    labels[i].style.color = Color.white;

                _lastMatchedWord = "";
                ClearTargetingPreview();
                RemoveClickOverlay();
            }
        }

        private void ShowWeaponRangePreview()
        {
            _gridVisual?.ClearHighlights();
            ClearCellClickHandlers();
            var ammoWords = _weaponService.GetAmmoWords(_playerId);
            if (ammoWords == null || ammoWords.Count == 0) return;

            // Find max range across all ammo words
            int maxRange = 0;
            foreach (var ammoWord in ammoWords)
            {
                var meta = _ammoResolver.GetStats(ammoWord);
                if (meta.Range > maxRange)
                    maxRange = meta.Range;
            }

            var firstAmmo = ammoWords[0];
            var playerPos = _combatGrid.GetPosition(_playerId);
            var inRange = _combatGrid.GetEntitiesInRange(playerPos, maxRange);
            foreach (var entity in inRange)
            {
                if (entity.Equals(_playerId)) continue;
                var pos = _combatGrid.GetPosition(entity);
                _gridVisual?.HighlightCells(new[] { pos }, HighlightEnemy);
                RegisterCellClick(pos, entity, firstAmmo);
            }
        }

        private void RegisterCellClick(GridPosition pos, EntityId? targetEntity, string wordToSubmit)
        {
            var cells = _gridVisual?.Cells;
            if (cells == null) return;
            var index = pos.Y * _gridWidth + pos.X;
            if (index < 0 || index >= cells.Count) return;

            var cell = cells[index];
            EventCallback<ClickEvent> cb = _ =>
            {
                if (targetEntity.HasValue)
                    _combatContext.SetFocusedTarget(targetEntity.Value);
                _combatContext.SetFocusedPosition(pos);

                SubmitCurrentWord(_isWeaponMode ? wordToSubmit : null);

                _combatContext.ClearFocusedTarget();
                _combatContext.ClearFocusedPosition();
            };
            cell.RegisterCallback(cb);
            _cellClickHandlers.Add((cell, cb));
        }

        private void ClearCellClickHandlers()
        {
            foreach (var (cell, cb) in _cellClickHandlers)
                cell.UnregisterCallback(cb);
            _cellClickHandlers.Clear();
        }

        private void ShowTargetingPreview(string text)
        {
            _gridVisual?.ClearHighlights();
            ClearCellClickHandlers();
            if (_previewService == null) return;

            var word = text.ToLowerInvariant();
            var previewService = _isWeaponMode ? _ammoPreviewService : _previewService;
            var preview = previewService.PreviewWord(word);
            if (preview.ActionPreviews.Count == 0) return;

            var sourceEntity = _combatContext.SourceEntity;
            var submitWord = _isWeaponMode ? _weaponService.GetAmmoWords(_playerId)?[0] ?? word : word;
            foreach (var actionPreview in preview.ActionPreviews)
            {
                foreach (var pos in actionPreview.AffectedPositions)
                {
                    var entityAt = _combatGrid.GetEntityAt(pos);
                    var color = entityAt.HasValue && entityAt.Value.Equals(sourceEntity)
                        ? HighlightSelf
                        : HighlightEnemy;
                    _gridVisual?.HighlightCells(new[] { pos }, color);
                    RegisterCellClick(pos, entityAt, submitWord);
                }
            }
        }

        private void ClearTargetingPreview()
        {
            _gridVisual?.ClearHighlights();
            ClearCellClickHandlers();
        }

        private void SubmitCurrentWord(string word = null)
        {
            if (!_isPlayerTurn || _gameOver) return;
            word ??= _codeField?.value?.Trim() ?? "";
            if (word.Length == 0) return;

            // Validate action before consuming turn
            bool validAction;
            if (_isWeaponMode)
                validAction = _weaponService.IsAmmoForEquipped(_playerId, word);
            else
                validAction = _wordResolver.HasWord(word);

            if (!validAction) return;

            _service.Clear();
            if (_isWeaponMode)
                _eventBus.Publish(new WeaponAmmoSubmittedEvent(_playerId, word));
            else
                _eventBus.Publish(new WordSubmittedEvent(word));
            _codeField.value = "";
            RecalculateFontSize();
            ClearTargetingPreview();
            RemoveClickOverlay();

            // Advance turns after player action
            AdvanceTurns();

            if (_isWeaponMode) ShowWeaponRangePreview();
            _codeField?.schedule.Execute(() => _codeField?.Focus());
        }

        private void CreateClickOverlay(string text)
        {
            RemoveClickOverlay();

            var labels = _codeField.CharLabels;
            if (labels.Count == 0) return;

            // Wait for layout to position overlay correctly
            _codeField.schedule.Execute(() =>
            {
                if (_codeField == null || labels.Count == 0) return;

                var firstLabel = labels[0];
                var lastLabel = labels[labels.Count - 1];
                var firstBound = firstLabel.worldBound;
                var lastBound = lastLabel.worldBound;

                if (float.IsNaN(firstBound.x) || float.IsNaN(lastBound.x)) return;

                // Convert world bounds to local coordinates in the main text panel
                var panelBound = _mainTextPanel.worldBound;
                var left = firstBound.x - panelBound.x;
                var top = firstBound.y - panelBound.y;
                var width = (lastBound.x + lastBound.width) - firstBound.x;
                var height = Mathf.Max(firstBound.height, lastBound.height);

                _clickOverlay = new VisualElement();
                _clickOverlay.style.position = Position.Absolute;
                _clickOverlay.style.left = left;
                _clickOverlay.style.top = top;
                _clickOverlay.style.width = width;
                _clickOverlay.style.height = height;
                _clickOverlay.style.backgroundColor = new Color(0, 0, 0, 0);
                _clickOverlay.pickingMode = PickingMode.Position;

                _clickOverlay.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    ShowTargetingPreview(text);
                });
                _clickOverlay.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    ClearTargetingPreview();
                });
                _clickOverlay.RegisterCallback<ClickEvent>(_ => SubmitCurrentWord());

                _mainTextPanel.Add(_clickOverlay);
            });
        }

        private void RemoveClickOverlay()
        {
            if (_clickOverlay != null)
            {
                _clickOverlay.RemoveFromHierarchy();
                _clickOverlay = null;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!_isPlayerTurn) return;
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCurrentWord();
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

        private void AdvanceTurns()
        {
            if (!_isPlayerTurn || _gameOver) return;

            _isPlayerTurn = false;
            Debug.Log("[Turn] Player's turn ended");
            SetInputEnabled(false);
            _turnService.EndTurn();

            while (true)
            {
                _turnService.BeginTurn();
                var current = _turnService.CurrentEntity;

                if (current.Equals(_playerId))
                {
                    _isPlayerTurn = true;
                    SetInputEnabled(true);
                    Debug.Log($"[Turn] === Player's turn (Turn #{_turnService.CurrentTurnNumber}, Round #{_turnService.CurrentRoundNumber}) ===");
                    break;
                }

                // EnemyAI.OnTurnStarted already processed this turn synchronously
                Debug.Log($"[Turn] {current.Value} turn processed");
                _turnService.EndTurn();

                if (_gameOver) return;
            }
        }

        private void SetInputEnabled(bool enabled)
        {
            if (_codeField != null)
                _codeField.SetEnabled(enabled);
        }

        private void UpdatePlayerHpBar()
        {
            if (_hpBar == null || _hpLabel == null) return;
            int hp = _entityStats.GetCurrentHealth(_playerId);
            int maxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            _hpBar.Value = (float)hp / maxHp;
            _hpLabel.text = $"{hp}/{maxHp}";
        }

        private void OnPlayerDied()
        {
            _gameOver = true;
            _isPlayerTurn = false;
            SetInputEnabled(false);
            Debug.Log("[Turn] GAME OVER — Player died!");
            _hpLabel.text = "DEAD";
            _hpLabel.style.color = Color.red;
        }

        private void OnWeaponEquipped(WeaponEquippedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Equipped: {evt.Weapon.DisplayName} (dur={evt.Weapon.Durability})");
            _weaponSlot.style.display = DisplayStyle.Flex;
            RenderWeaponName(evt.Weapon.DisplayName);
            _weaponDurabilityLabel.text = evt.Weapon.Durability.ToString();
        }

        private void OnWeaponDurabilityChanged(WeaponDurabilityChangedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Durability: {evt.CurrentDurability}/{evt.MaxDurability}");
            _weaponDurabilityLabel.text = evt.CurrentDurability.ToString();
        }

        private void OnWeaponDestroyed(WeaponDestroyedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Weapon destroyed: {evt.WeaponWord}");
            _weaponSlot.style.display = DisplayStyle.None;
            _isWeaponMode = false;
            _weaponNameContainer.Clear();
        }

        private void RenderWeaponName(string name)
        {
            _weaponNameContainer.Clear();
            // Use resolved slot dimensions, matching how UnitGridVisual renders entity names
            float cellWidth = _weaponSlot.resolvedStyle.width;
            float cellHeight = _weaponSlot.resolvedStyle.height;
            if (float.IsNaN(cellWidth) || cellWidth <= 0) cellWidth = 80f;
            if (float.IsNaN(cellHeight) || cellHeight <= 0) cellHeight = 80f;
            // Reserve space for durability label at bottom
            cellHeight -= 20f;
            var layout = UnitTextLayout.Calculate(name, cellWidth, cellHeight);
            foreach (var rowText in layout.Rows)
            {
                var label = new Label(rowText);
                label.style.fontSize = layout.FontSize;
                label.style.color = Color.white;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginTop = 0;
                label.style.marginBottom = 0;
                label.style.paddingTop = 0;
                label.style.paddingBottom = 0;
                _weaponNameContainer.Add(label);
            }
        }

        private void UpdateWeaponSlotBorder()
        {
            var color = _isWeaponMode ? WeaponSlotBorderActive : WeaponSlotBorderDefault;
            _weaponSlot.style.borderTopColor = color;
            _weaponSlot.style.borderBottomColor = color;
            _weaponSlot.style.borderLeftColor = color;
            _weaponSlot.style.borderRightColor = color;
        }

        private void RefreshEntityCell(EntityId entityId)
        {
            try
            {
                var pos = _combatGrid.GetPosition(entityId);
                _gridVisual?.UpdateCell(pos);
            }
            catch (KeyNotFoundException) { }
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var expectedCellCount = _gridWidth * _gridHeight;
            var actualCellCount = _gridVisual?.Cells.Count ?? 0;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _codeField != null,
                    _codeField != null ? null : "Code field is null"),
                new($"Grid cells spawned ({expectedCellCount})",
                    actualCellCount == expectedCellCount,
                    actualCellCount == expectedCellCount
                        ? null : $"Expected {expectedCellCount}, got {actualCellCount}"),
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

            (_enemyAI as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _eventBus?.ClearAllSubscriptions();
            _enemyAI = null;
            _turnService = null;
            _encounterAdapter = null;
            _eventBus = null;
            _service = null;
            _unitService = null;
            _codeField = null;
            _linesContainer = null;
            _mainTextPanel = null;
            _statsBar = null;
            _tileMapPanel = null;
            _gridVisual = null;
            _wordMatchService = null;
            _wordResolver = null;
            _previewService = null;
            _ammoPreviewService = null;
            _combatContext = null;
            _combatGrid = null;
            _entityStats = null;
            _clickOverlay = null;
            _weaponService = null;
            _weaponExecutor = null;
            _actionExecution = null;
            _ammoResolver = null;
            _ammoMatchService = null;
            _weaponSlot = null;
            _weaponDurabilityLabel = null;
            _weaponNameContainer = null;
            _hpBar = null;
            _hpLabel = null;
        }
    }
}
