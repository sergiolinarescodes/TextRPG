using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.CombatAI;
using TextRPG.Core.Equipment;
using TextRPG.Core.Passive;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
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

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private IEventBus _eventBus;
        private IWordInputService _service;
        private IUnitService _unitService;
        private AnimatedCodeField _codeField;
        private VisualElement _mainTextPanel;
        private VisualElement _statsBar;
        private float _fontScaleFactor;
        private VisualElement _linesContainer;
        private readonly List<IDisposable> _subscriptions = new();
        private CombatSlotVisual _slotVisual;
        private IWordMatchService _wordMatchService;
        private IWordResolver _wordResolver;
        private ITargetingPreviewService _previewService;
        private ITargetingPreviewService _ammoPreviewService;
        private ICombatContext _combatContext;
        private ICombatSlotService _slotService;
        private IEntityStatsService _entityStats;

        private IWeaponService _weaponService;
        private IWeaponActionExecutor _weaponExecutor;
        private IActionExecutionService _actionExecution;
        private IWordResolver _ammoResolver;
        private IWordMatchService _ammoMatchService;
        private EquipmentBarVisual _leftBar;
        private EquipmentBarVisual _rightBar;
        private VisualElement _allyRow;
        private VisualElement _weaponSlot;
        private Label _weaponDurabilityLabel;
        private EntityId _playerId;
        private string _lastMatchedWord = "";

        private ITurnService _turnService;
        private ICombatAIService _combatAI;
        private ActionAnimationService _animationService;
        private StatusEffectVisualService _statusVisualService;
        private IStatusEffectService _statusEffects;
        private ScenarioEncounterAdapter _encounterAdapter;
        private IPassiveService _passiveService;
        private ICombatLoopService _combatLoop;
        private IEquipmentService _equipmentService;
        private IInventoryService _inventoryService;
        private IItemRegistry _itemRegistry;
        private InventoryId _playerInventoryId;
        private VisualElement _dragElement;
        private string _dragItemWord;
        private int _dragSourceSlot = -1;
        private bool _dragFromEquipment;
        private UnidadProgressBar _hpBar;
        private Label _hpLabel;
        private UnidadProgressBar _manaBar;
        private Label _manaLabel;
        private VisualElement _manaCostOverlay;
        private bool _isPreviewingManaCost;
        private ILootRewardService _lootRewardService;
        private VisualElement _lootOverlay;
        private VisualElement _lootTooltip;
        private VisualElement _tooltipLayer;

        private static readonly Color HighlightEnemy = new(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HighlightSelf = new(0.3f, 1f, 0.3f, 0.4f);

        public WordInputScenario() : base(new TestScenarioDefinition(
            "word-input",
            "Word Input (Live)",
            "Full-screen word input with auto-scaling text, vibration animation, " +
            "slot-based combat, and stats bar. Type a word and press Enter to submit.",
            new[] { VibrationAmplitudeParam, FontScaleFactorParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
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

            // CombatSlot + CombatContext
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            _combatContext = new CombatContext();
            _combatContext.SetEntityStats(_entityStats);
            _combatContext.SetSlotService(_slotService);

            // Turn system
            _turnService = new TurnService(_eventBus);

            // StatusEffect
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            var statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;
            _statusEffects = statusEffects;

            // Weapon service + action execution
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordActionData);
            _weaponService = new WeaponService(_eventBus, weaponRegistry);

            // Item/Equipment registry
            _itemRegistry = EquipmentSystemInstaller.BuildItemRegistry(wordActionData);

            // Inventory service
            _inventoryService = new InventoryService(_eventBus);
            _playerInventoryId = new InventoryId("player");

            // Full action handler registry
            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, _combatContext,
                statusEffects, _turnService, _weaponService, slotService: _slotService);
            var handlerRegistry = ActionHandlerRegistryFactory.CreateDefault(actionHandlerCtx);

            // Player entity (not in slots — first person)
            _playerId = new EntityId("player");
            _entityStats.RegisterEntity(_playerId, 100, 10, 8, 5, 4, 3);

            // Create player inventory
            _inventoryService.Create(_playerInventoryId, new InventoryDefinition(EquipmentConstants.InventorySlotCount));

            // Define inventory items from item registry
            foreach (var itemWord in _itemRegistry.Keys)
            {
                if (_itemRegistry.TryGet(itemWord, out var itemDef))
                    _inventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(new ItemId(itemWord), itemDef.DisplayName, 1));
            }

            // Item handler registered later (after _equipmentService is created)

            // Load enemy definitions from DB
            var allUnits = UnitDatabaseLoader.LoadAll();
            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(_playerId);
            _encounterAdapter.SetEventBus(_eventBus);
            var enemyResolver = new EnemyWordResolver();

            var enemySpawns = new[] { "goblin", "skeleton", "bat" };

            var enemyIds = new List<EntityId>();
            for (int i = 0; i < enemySpawns.Length && i < 3; i++)
            {
                var unitId = enemySpawns[i];
                if (!allUnits.TryGetValue(unitId, out var def)) continue;

                var entityId = new EntityId(unitId);
                _entityStats.RegisterEntity(entityId, def.MaxHealth, def.Strength, def.MagicPower,
                    def.PhysicalDefense, def.MagicDefense, def.Luck);
                _unitService.Register(new UnitId(unitId),
                    new UnitDefinition(new UnitId(unitId), def.Name,
                        def.MaxHealth, def.Strength, def.PhysicalDefense, def.Luck, def.Color));
                _slotService.RegisterEnemy(entityId, i);
                enemyIds.Add(entityId);
                _encounterAdapter.RegisterEnemy(entityId, def);
                UnitDatabaseLoader.RegisterUnitWords(enemyResolver, unitId);
            }
            _encounterAdapter.Activate();

            _combatContext.SetSourceEntity(_playerId);
            _combatContext.SetEnemies(enemyIds.ToArray());
            _combatContext.SetAllies(Array.Empty<EntityId>());

            _previewService = new TargetingPreviewService(_wordResolver, _combatContext);
            _ammoPreviewService = new TargetingPreviewService(_ammoResolver, _combatContext);

            // Composite resolver for AI action execution
            var compositeResolver = new CompositeWordResolver(_wordResolver, enemyResolver);
            var scenarioAnimResolver = new ScenarioAnimationResolver();
            _actionExecution = new ActionExecutionService(_eventBus, compositeResolver, handlerRegistry, _combatContext, _entityStats, statusEffects, scenarioAnimResolver);

            // Weapon executor
            _weaponExecutor = new WeaponActionExecutor(
                _eventBus, _weaponService, _ammoResolver, handlerRegistry, _combatContext, scenarioAnimResolver);

            // Action Animation service (created early so PassiveContext can reference it)
            _animationService = new ActionAnimationService(_eventBus, scenarioAnimResolver, handlerRegistry, _entityStats);

            // Passive system
            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry();
            var targetResolver = new PassiveTargetResolver();
            var passiveContext = new PassiveContext(_entityStats, _slotService, _eventBus, _encounterAdapter,
                animationService: _animationService);
            _passiveService = new PassiveService(_eventBus, triggerRegistry, effectRegistry, targetResolver, passiveContext, allUnits);

            // Equipment service (needs passive service)
            _equipmentService = new EquipmentService(_eventBus, _itemRegistry, _entityStats, _passiveService, _weaponService);

            // Register Item action handler (needs _equipmentService + _itemRegistry for auto-equip)
            handlerRegistry.Register("Item", new ItemActionHandler(actionHandlerCtx, _inventoryService, _playerInventoryId, _equipmentService, _itemRegistry));

            // Loot reward service
            _lootRewardService = new LootRewardService(_eventBus, _itemRegistry, _inventoryService, _playerInventoryId, _playerId);

            // Register passives for initial enemies
            foreach (var unitId in enemySpawns)
            {
                if (allUnits.TryGetValue(unitId, out var unitDef) && unitDef.Passives != null)
                    _passiveService.RegisterPassives(new EntityId(unitId), unitDef.Passives);
            }

            // Scorer registry
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(statusEffects);

            // CombatAI service
            _combatAI = new CombatAIService(_eventBus, _encounterAdapter, _entityStats,
                _turnService, _slotService, _combatContext, _actionExecution, scorers, enemyResolver, allUnits);

            _statusVisualService = new StatusEffectVisualService(_eventBus);

            var tickRunner = SceneRoot.AddComponent<TickRunner>();
            tickRunner.Initialize(new UnityTimeProvider(), new ITickable[] { _animationService });

            // Turn order: player first, then enemies
            var turnOrder = new List<EntityId> { _playerId };
            turnOrder.AddRange(enemyIds);
            _turnService.SetTurnOrder(turnOrder);

            // CombatLoop — orchestrates turn sequencing, word submission, game-over
            _combatLoop = new CombatLoopService(
                _eventBus, _turnService, _entityStats, _wordResolver, _weaponService, _playerId);
            _combatLoop.Start();

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
                    Debug.Log($"[WordInputScenario] \"{evt.Word}\" → {meta.Target} cost={meta.Cost} | {actionList}");
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

            // Re-render slot cells when HP changes + update player HP bar
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
                if (evt.EntityId.Equals(_playerId))
                    RefreshEntityCell(evt.EntityId);
                else
                    _slotVisual?.PlayDeathAnimation(evt.EntityId);
                // Ally slots always visible — placeholders shown by PlayDeathAnimation clearing the cell
            }));

            // Mana bar updates
            _subscriptions.Add(_eventBus.Subscribe<ManaChangedEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId))
                {
                    if (_isPreviewingManaCost)
                        ClearManaCostPreview();
                    else
                        UpdatePlayerManaBar();
                }
            }));
            _subscriptions.Add(_eventBus.Subscribe<WordRejectedEvent>(evt =>
            {
                Debug.Log($"[WordInputScenario] Insufficient mana for \"{evt.Word}\" (cost={evt.ManaCost})");
            }));

            // Log passive triggers
            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(evt =>
            {
                Debug.Log($"[Passive] {evt.TriggerId}+{evt.EffectId} from {evt.SourceEntity.Value} → " +
                          $"value={evt.Value} affected={evt.AffectedEntity?.Value ?? "none"}");
            }));

            // Log enemy actions
            _subscriptions.Add(_eventBus.Subscribe<ActionHandlerExecutedEvent>(evt =>
            {
                if (!_combatLoop.IsPlayerTurn)
                    Debug.Log($"[Turn:Enemy] {evt.ActionId}({evt.Value}) → {evt.Targets.Count} target(s)");
            }));

            // CombatLoop events — presentation responses
            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
            {
                SetInputEnabled(true);
                Debug.Log($"[Turn] === Player's turn (Turn #{evt.TurnNumber}, Round #{evt.RoundNumber}) ===");
            }));
            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnEndedEvent>(_ =>
            {
                SetInputEnabled(false);
                Debug.Log("[Turn] Player's turn ended");
            }));
            _subscriptions.Add(_eventBus.Subscribe<GameOverEvent>(_ =>
            {
                SetInputEnabled(false);
                _hpLabel.text = "DEAD";
                _hpLabel.style.color = Color.red;
                Debug.Log("[Turn] GAME OVER — Player died!");
            }));

            // Register summoned units with the unit service for proper text rendering
            _subscriptions.Add(_eventBus.Subscribe<UnitSummonedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var uid = new UnitId(evt.EntityId.Value);
                if (allUnits.TryGetValue(word, out var unitDef))
                {
                    _unitService.Register(uid,
                        new UnitDefinition(uid, unitDef.Name,
                            unitDef.MaxHealth, unitDef.Strength, unitDef.PhysicalDefense, unitDef.Luck, unitDef.Color));
                }
                else
                {
                    _unitService.Register(uid,
                        new UnitDefinition(uid, evt.EntityId.Value.ToUpperInvariant(),
                            _entityStats.GetStat(evt.EntityId, StatType.MaxHealth), 0, 0, 0, Color.white));
                }
            }));

            // Summon visual registration + ally row visibility
            _subscriptions.Add(_eventBus.Subscribe<SlotEntityRegisteredEvent>(evt =>
            {
                if (_slotVisual != null)
                {
                    var slotElements = _slotVisual.GetAllSlotElements();
                    int visualIndex = evt.Slot.Type == SlotType.Enemy ? evt.Slot.Index : 3 + evt.Slot.Index;
                    if (visualIndex >= 0 && visualIndex < slotElements.Count)
                    {
                        _slotVisual.RegisterEntity(evt.EntityId, slotElements[visualIndex]);
                    }
                }
                // Ally row always visible — no display toggle needed
            }));

            // Loot reward events
            _subscriptions.Add(_eventBus.Subscribe<LootRewardOfferedEvent>(evt => ShowLootSelection(evt.Options)));
            _subscriptions.Add(_eventBus.Subscribe<LootRewardSelectedEvent>(_ => HideLootSelection()));

            // --- Build UI ---
            BuildUI(vibrationAmplitude);

            Debug.Log($"[WordInputScenario] Started — vibration={vibrationAmplitude}, fontScale={_fontScaleFactor}");
        }

        private void BuildUI(float vibrationAmplitude)
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            // Enemy row
            var enemyRow = new VisualElement();
            enemyRow.style.flexDirection = FlexDirection.Row;
            enemyRow.style.justifyContent = Justify.Center;
            enemyRow.style.alignItems = Align.Center;
            enemyRow.style.height = Length.Percent(20);
            enemyRow.style.backgroundColor = Color.black;
            root.Add(enemyRow);

            _slotVisual = new CombatSlotVisual(_unitService, _entityStats, _slotService);
            _slotVisual.BuildEnemyRow(enemyRow);

            // Middle area: left bar | text panel | right bar
            var middleArea = new VisualElement();
            middleArea.style.flexDirection = FlexDirection.Row;
            middleArea.style.flexGrow = 1;

            _leftBar = new EquipmentBarVisual(EquipmentConstants.InventorySlotCount);
            _leftBar.BuildColumn(middleArea);

            // Main text panel
            _mainTextPanel = new VisualElement();
            _mainTextPanel.style.flexGrow = 1;
            _mainTextPanel.style.backgroundColor = Color.black;
            _mainTextPanel.style.justifyContent = Justify.Center;
            _mainTextPanel.style.alignItems = Align.Stretch;
            _mainTextPanel.style.overflow = Overflow.Hidden;
            middleArea.Add(_mainTextPanel);

            _rightBar = new EquipmentBarVisual(EquipmentConstants.SlotCount);
            _rightBar.BuildColumn(middleArea);

            // Set equipment slot placeholders on right bar
            _rightBar.SetSlotBackground(0, "HEAD", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(1, "WEAR", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(2, "ACCES SORY", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(3, "TRIN KET", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(4, "WEAPON", SlotColors.Placeholder);

            // Set inventory slot placeholders on left bar
            for (int i = 0; i < EquipmentConstants.InventorySlotCount; i++)
                _leftBar.SetSlotBackground(i, "INVEN TORY", SlotColors.Placeholder);

            root.Add(middleArea);

            // AnimatedCodeField
            _codeField = new AnimatedCodeField();
            _codeField.multiline = false;
            _codeField.PersistentFocus = true;
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

            _linesContainer = _codeField.Q(className: "animated-code-field__lines");
            if (_linesContainer != null)
            {
                _linesContainer.style.paddingLeft = 0;
                _linesContainer.style.paddingTop = 0;
                _linesContainer.style.paddingRight = 0;
                _linesContainer.style.paddingBottom = 0;
                _linesContainer.style.justifyContent = Justify.Center;
            }

            _codeField.RegisterValueChangedCallback(OnTextChanged);
            _codeField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _mainTextPanel.RegisterCallback<GeometryChangedEvent>(_ => RecalculateFontSize());

            // Ally row (always visible, positioned at bottom of text panel)
            _allyRow = new VisualElement();
            _allyRow.pickingMode = PickingMode.Ignore;
            _allyRow.style.position = Position.Absolute;
            _allyRow.style.bottom = 10;
            _allyRow.style.left = 0;
            _allyRow.style.right = 0;
            _allyRow.style.flexDirection = FlexDirection.Row;
            _allyRow.style.justifyContent = Justify.Center;
            _allyRow.style.alignItems = Align.Center;
            _allyRow.style.height = 100;
            _allyRow.style.paddingTop = 4;
            _allyRow.style.paddingBottom = 4;
            _mainTextPanel.Add(_allyRow);

            _slotVisual.BuildAllyRow(_allyRow);

            // Stats bar
            _statsBar = new VisualElement();
            _statsBar.style.flexGrow = 0;
            _statsBar.style.flexShrink = 0;
            _statsBar.style.height = 150;
            _statsBar.style.backgroundColor = Color.black;
            _statsBar.style.flexDirection = FlexDirection.Column;
            _statsBar.style.justifyContent = Justify.Center;
            _statsBar.style.paddingLeft = 10;
            _statsBar.style.paddingRight = 10;
            root.Add(_statsBar);

            // HP bar
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

            // Mana bar
            var manaBarWrapper = new VisualElement();
            manaBarWrapper.style.width = Length.Percent(100);
            manaBarWrapper.style.height = 48;
            manaBarWrapper.style.marginBottom = 8;
            manaBarWrapper.style.justifyContent = Justify.Center;

            _manaBar = new UnidadProgressBar(0.5f);
            _manaBar.SetVariant(ProgressVariant.Info);
            _manaBar.style.width = Length.Percent(100);
            _manaBar.style.height = 16;
            manaBarWrapper.Add(_manaBar);

            _manaCostOverlay = new VisualElement();
            _manaCostOverlay.style.position = Position.Absolute;
            _manaCostOverlay.style.top = 0;
            _manaCostOverlay.style.bottom = 0;
            _manaCostOverlay.style.display = DisplayStyle.None;
            _manaCostOverlay.pickingMode = PickingMode.Ignore;
            _manaBar.Add(_manaCostOverlay);

            int playerMana = _entityStats.GetCurrentMana(_playerId);
            int playerMaxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            _manaLabel = new Label($"{playerMana}/{playerMaxMana}");
            _manaLabel.style.position = Position.Absolute;
            _manaLabel.style.top = 0;
            _manaLabel.style.left = 0;
            _manaLabel.style.right = 0;
            _manaLabel.style.bottom = 0;
            _manaLabel.style.fontSize = 36;
            _manaLabel.style.color = Color.white;
            _manaLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _manaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            manaBarWrapper.Add(_manaLabel);

            _statsBar.Add(manaBarWrapper);

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

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            // Position provider
            Func<EntityId, Vector3> positionProvider = entityId =>
            {
                if (entityId.Equals(_playerId))
                {
                    var hpCenter = _hpBar.worldBound.center;
                    return new Vector3(hpCenter.x, hpCenter.y, 0f);
                }
                var element = _slotVisual.GetSlotElement(entityId);
                if (element != null)
                {
                    var center = element.worldBound.center;
                    return new Vector3(center.x, center.y, 0f);
                }
                return Vector3.zero;
            };
            _animationService.Initialize(positionProvider, projectileOverlay);

            // Status effect visual overlays
            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);

            _tooltipLayer = new VisualElement { name = "tooltip-layer" };
            _tooltipLayer.style.position = Position.Absolute;
            _tooltipLayer.style.left = 0;
            _tooltipLayer.style.top = 0;
            _tooltipLayer.style.right = 0;
            _tooltipLayer.style.bottom = 0;
            _tooltipLayer.pickingMode = PickingMode.Ignore;
            root.Add(_tooltipLayer);

            _statusVisualService.Initialize(positionProvider, statusTextOverlay,
                _slotVisual.GetAllSlotElements(), _slotService, _statusEffects, _unitService,
                _slotVisual, _tooltipLayer, _passiveService);

            // Weapon slot — uses bottom slot of right bar
            _weaponSlot = _rightBar.GetSlotElement(4);
            _weaponSlot.pickingMode = PickingMode.Position;

            _weaponDurabilityLabel = new Label();
            _weaponDurabilityLabel.style.color = new Color(1f, 0.8f, 0.2f);
            _weaponDurabilityLabel.style.fontSize = 20;
            _weaponDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _weaponDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _weaponDurabilityLabel.style.position = Position.Absolute;
            _weaponDurabilityLabel.style.bottom = 2;
            _weaponDurabilityLabel.style.left = 4;

            _weaponSlot.RegisterCallback<ClickEvent>(_ => FireWeapon());

            // --- Inventory/Equipment visual subscriptions ---
            _subscriptions.Add(_eventBus.Subscribe<Unidad.Core.Inventory.SlotChangedEvent>(OnInventorySlotChanged));
            _subscriptions.Add(_eventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped));
            _subscriptions.Add(_eventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped));

            // --- Drag-and-drop on inventory (left) and equipment (right) slots ---
            RegisterSlotDragHandlers(_leftBar, EquipmentConstants.InventorySlotCount, OnInventorySlotPointerDown);
            RegisterSlotDragHandlers(_rightBar, EquipmentConstants.SlotCount, OnEquipmentSlotPointerDown);

            // Pointer move/up on root for drag tracking
            root.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnDragPointerUp);

            _codeField.schedule.Execute(() => _codeField.Focus());
        }

        private bool IsAmmoWord(string word) =>
            _weaponService != null && _weaponService.HasWeapon(_playerId)
            && _weaponService.IsAmmoForEquipped(_playerId, word);

        private void FireWeapon()
        {
            if (!_combatLoop.FireWeapon()) return;

            _service.Clear();
            _codeField.value = "";
            ClearTargetingPreview();
            ClearManaCostPreview();
            _codeField?.schedule.Execute(() => _codeField?.Focus());
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            var newText = evt.newValue ?? "";

            _service.Clear();
            foreach (var c in newText)
                _service.AppendCharacter(c);

            RecalculateFontSize();

            bool isAmmo = IsAmmoWord(newText);
            var matchService = isAmmo ? _ammoMatchService : _wordMatchService;
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

                var currentWord = newText.ToLowerInvariant();
                if (currentWord != _lastMatchedWord)
                {
                    _lastMatchedWord = currentWord;
                    ShowTargetingPreview(newText);
                    if (!isAmmo)
                        ShowManaCostPreview(currentWord);
                    else
                        ClearManaCostPreview();
                }
            }
            else
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < labels.Count; i++)
                    labels[i].style.color = Color.white;

                _lastMatchedWord = "";
                ClearTargetingPreview();
                ClearManaCostPreview();
            }
        }

        private void ShowTargetingPreview(string text)
        {
            ClearTargetingPreview();
            if (_previewService == null) return;

            var word = text.ToLowerInvariant();
            var previewService = IsAmmoWord(word) ? _ammoPreviewService : _previewService;
            var preview = previewService.PreviewWord(word);
            if (preview.ActionPreviews.Count == 0) return;

            var sourceEntity = _combatContext.SourceEntity;
            foreach (var actionPreview in preview.ActionPreviews)
            {
                foreach (var entityId in actionPreview.AffectedEntities)
                {
                    var element = _slotVisual.GetSlotElement(entityId);
                    if (element == null) continue;
                    var color = entityId.Equals(sourceEntity) ? HighlightSelf : HighlightEnemy;
                    element.style.backgroundColor = color;
                }
            }
        }

        private void ClearTargetingPreview()
        {
            // Clear all slot highlights
            var elements = _slotVisual?.GetAllSlotElements();
            if (elements == null) return;
            for (int i = 0; i < elements.Count; i++)
                elements[i].style.backgroundColor = Color.black;
        }

        private void SubmitCurrentWord(string word = null)
        {
            word ??= _codeField?.value?.Trim() ?? "";
            if (word.Length == 0) return;

            var result = _combatLoop.SubmitWord(word);
            if (result == WordSubmitResult.InsufficientMana)
            {
                PlayManaRejection();
                _codeField?.schedule.Execute(() => _codeField?.Focus());
                return;
            }
            if (result != WordSubmitResult.Accepted) return;

            ClearManaCostPreview();
            _service.Clear();
            _codeField.value = "";
            RecalculateFontSize();
            ClearTargetingPreview();
            _codeField?.schedule.Execute(() => _codeField?.Focus());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!_combatLoop.IsPlayerTurn) return;
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitCurrentWord();
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private void RecalculateFontSize()
        {
            if (_codeField == null || _mainTextPanel == null) return;

            var widthSource = _linesContainer ?? (VisualElement)_mainTextPanel;
            var panelWidth = widthSource.resolvedStyle.width;
            if (float.IsNaN(panelWidth) || panelWidth <= 0) return;

            var text = _codeField.value ?? "";
            var charCount = Mathf.Max(text.Length, 1);

            var ratio = _codeField.BaseCharWidthRatio;
            var fontSize = panelWidth / (charCount * ratio);
            fontSize = Mathf.Clamp(fontSize, 12f, 800f);
            _codeField.SetCharFontSize(fontSize);

            if (charCount > 0 && text.Length > 0)
            {
                var labels = _codeField.CharLabels;
                if (labels.Count == 0) return;

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

        private void SetInputEnabled(bool enabled)
        {
            if (_codeField != null)
            {
                _codeField.PersistentFocus = enabled;
                _codeField.SetEnabled(enabled);
                if (enabled)
                    _codeField.schedule.Execute(() => _codeField?.Focus());
            }
        }

        private void UpdatePlayerHpBar()
        {
            if (_hpBar == null || _hpLabel == null) return;
            int hp = _entityStats.GetCurrentHealth(_playerId);
            int maxHp = _entityStats.GetStat(_playerId, StatType.MaxHealth);
            _hpBar.Value = (float)hp / maxHp;
            _hpLabel.text = $"{hp}/{maxHp}";
        }

        private void UpdatePlayerManaBar()
        {
            if (_manaBar == null || _manaLabel == null) return;
            int mana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            _manaBar.Value = maxMana > 0 ? (float)mana / maxMana : 0f;
            _manaLabel.text = $"{mana}/{maxMana}";
        }

        private void ShowManaCostPreview(string word)
        {
            if (_manaBar == null || _manaLabel == null || _manaCostOverlay == null) return;

            var meta = _wordResolver.GetStats(word);
            int cost = meta.Cost;
            if (cost <= 0)
            {
                ClearManaCostPreview();
                return;
            }

            _isPreviewingManaCost = true;
            int currentMana = _entityStats.GetCurrentMana(_playerId);
            int maxMana = _entityStats.GetStat(_playerId, StatType.MaxMana);
            if (maxMana <= 0) return;

            float currentRatio = (float)currentMana / maxMana;
            int previewMana = currentMana - cost;
            float previewRatio = (float)previewMana / maxMana;

            _manaBar.Value = Mathf.Clamp01(previewRatio);

            float overlayLeft = Mathf.Max(0f, previewRatio);
            float overlayWidth = currentRatio - overlayLeft;
            _manaCostOverlay.style.left = Length.Percent(overlayLeft * 100f);
            _manaCostOverlay.style.width = Length.Percent(overlayWidth * 100f);
            _manaCostOverlay.style.display = DisplayStyle.Flex;

            bool canAfford = previewMana >= 0;
            _manaCostOverlay.style.backgroundColor = canAfford
                ? new Color(1f, 0.6f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);

            _manaLabel.text = $"{previewMana}/{maxMana} (-{cost})";
            _manaLabel.style.color = canAfford ? Color.white : Color.red;
        }

        private void ClearManaCostPreview()
        {
            if (!_isPreviewingManaCost) return;
            _isPreviewingManaCost = false;
            if (_manaCostOverlay != null)
                _manaCostOverlay.style.display = DisplayStyle.None;
            UpdatePlayerManaBar();
            if (_manaLabel != null)
                _manaLabel.style.color = Color.white;
        }

        private void PlayManaRejection()
        {
            var labels = _codeField.CharLabels;
            var indices = new List<int>();
            for (int i = 0; i < labels.Count; i++)
                indices.Add(i);
            _codeField.PlayRejectionAnimation(indices);

            _manaBar.SetVariant(ProgressVariant.Danger);
            _manaBar.schedule.Execute(() => _manaBar?.SetVariant(ProgressVariant.Info)).ExecuteLater(500);

            Debug.Log("[WordInputScenario] Insufficient mana — word rejected");
        }

        private void OnWeaponEquipped(WeaponEquippedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Equipped: {evt.Weapon.DisplayName} (dur={evt.Weapon.Durability})");
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
            _rightBar?.ClearSlotContent(4);
            if (_weaponDurabilityLabel?.parent != null)
                _weaponDurabilityLabel.RemoveFromHierarchy();
        }

        private void RenderWeaponName(string name)
        {
            _rightBar?.SetSlotContent(4, name, Color.white);
            _weaponSlot?.Add(_weaponDurabilityLabel);
        }

        private void RefreshEntityCell(EntityId entityId)
        {
            _slotVisual?.RefreshSlot(entityId);
        }

        // --- Inventory/Equipment Visual Handlers ---

        private void OnInventorySlotChanged(Unidad.Core.Inventory.SlotChangedEvent evt)
        {
            if (evt.InventoryId != _playerInventoryId) return;
            if (evt.NewSlot.IsEmpty)
            {
                _leftBar?.ClearSlotContent(evt.SlotIndex);
            }
            else
            {
                var itemWord = evt.NewSlot.ItemId.Value;
                if (_itemRegistry.TryGet(itemWord, out var itemDef))
                    _leftBar?.SetSlotContent(evt.SlotIndex, itemDef.DisplayName, itemDef.Color);
                else
                    _leftBar?.SetSlotContent(evt.SlotIndex, itemWord.ToUpperInvariant(), Color.white);
            }
        }

        private void OnItemEquipped(ItemEquippedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            int slotIndex = (int)evt.Slot;
            _rightBar?.SetSlotContent(slotIndex, evt.Item.DisplayName, evt.Item.Color);
            Debug.Log($"[Equipment] Equipped {evt.Item.DisplayName} → {evt.Slot}");
        }

        private void OnItemUnequipped(ItemUnequippedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            int slotIndex = (int)evt.Slot;
            _rightBar?.ClearSlotContent(slotIndex);
            Debug.Log($"[Equipment] Unequipped {evt.Item.DisplayName} from {evt.Slot}");
        }

        // --- Drag-and-Drop ---

        private static void RegisterSlotDragHandlers(EquipmentBarVisual bar, int count, Action<PointerDownEvent, int> handler)
        {
            for (int i = 0; i < count; i++)
            {
                var slotIndex = i;
                var slotElement = bar.GetSlotElement(i);
                slotElement.pickingMode = PickingMode.Position;
                slotElement.RegisterCallback<PointerDownEvent>(evt => handler(evt, slotIndex));
            }
        }

        private void OnInventorySlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (_encounterAdapter?.IsEncounterActive == true) return;
            if (_inventoryService == null) return;
            var slot = _inventoryService.GetSlot(_playerInventoryId, slotIndex);
            if (slot.IsEmpty) return;

            _dragItemWord = slot.ItemId.Value;
            _dragSourceSlot = slotIndex;
            _dragFromEquipment = false;
            StartDrag(evt.position);
            evt.StopPropagation();
        }

        private void OnEquipmentSlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (_encounterAdapter?.IsEncounterActive == true) return;
            if (_equipmentService == null) return;
            var slotType = (EquipmentSlotType)slotIndex;
            var equipped = _equipmentService.GetEquipped(_playerId, slotType);
            if (equipped == null) return;

            _dragItemWord = equipped.ItemWord;
            _dragSourceSlot = slotIndex;
            _dragFromEquipment = true;
            StartDrag(evt.position);
            evt.StopPropagation();
        }

        private void StartDrag(Vector2 position)
        {
            if (_dragElement != null) return;

            _dragElement = new VisualElement();
            _dragElement.style.position = Position.Absolute;
            _dragElement.style.width = 100;
            _dragElement.style.height = 80;
            _dragElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.85f);
            _dragElement.style.borderTopWidth = 2;
            _dragElement.style.borderBottomWidth = 2;
            _dragElement.style.borderLeftWidth = 2;
            _dragElement.style.borderRightWidth = 2;
            _dragElement.style.borderTopColor = Color.yellow;
            _dragElement.style.borderBottomColor = Color.yellow;
            _dragElement.style.borderLeftColor = Color.yellow;
            _dragElement.style.borderRightColor = Color.yellow;
            _dragElement.style.justifyContent = Justify.Center;
            _dragElement.style.alignItems = Align.Center;
            _dragElement.pickingMode = PickingMode.Ignore;

            var label = new Label(_dragItemWord.ToUpperInvariant());
            label.style.color = Color.white;
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.pickingMode = PickingMode.Ignore;
            _dragElement.Add(label);

            RootVisualElement.Add(_dragElement);
            UpdateDragPosition(position);
        }

        private void UpdateDragPosition(Vector2 position)
        {
            if (_dragElement == null) return;
            _dragElement.style.left = position.x - 50;
            _dragElement.style.top = position.y - 40;
        }

        private void OnDragPointerMove(PointerMoveEvent evt)
        {
            if (_dragElement == null) return;
            UpdateDragPosition(evt.position);
        }

        private void OnDragPointerUp(PointerUpEvent evt)
        {
            if (_dragElement == null || _dragItemWord == null) return;

            bool handled;
            if (_dragFromEquipment)
                handled = _equipmentService.UnequipToInventory(
                    _playerId, (EquipmentSlotType)_dragSourceSlot, _inventoryService, _playerInventoryId);
            else
                handled = TryDropToEquipmentSlot(evt.position);

            if (!handled)
                Debug.Log($"[Equipment] Drag cancelled for {_dragItemWord}");

            CancelDrag();
        }

        private bool TryDropToEquipmentSlot(Vector2 dropPos)
        {
            for (int i = 0; i < EquipmentConstants.SlotCount; i++)
            {
                var slotElement = _rightBar.GetSlotElement(i);
                if (slotElement == null || !slotElement.worldBound.Contains(dropPos)) continue;

                var targetSlotType = (EquipmentSlotType)i;
                var itemSlotType = _equipmentService.GetSlotTypeForItem(_dragItemWord);
                if (itemSlotType == null || itemSlotType.Value != targetSlotType)
                {
                    Debug.Log($"[Equipment] {_dragItemWord} doesn't fit in {targetSlotType} slot");
                    return false;
                }

                return _equipmentService.EquipFromInventory(_playerId, _dragItemWord, _inventoryService, _playerInventoryId);
            }
            return false;
        }

        private void CancelDrag()
        {
            _dragElement?.RemoveFromHierarchy();
            _dragElement = null;
            _dragItemWord = null;
            _dragSourceSlot = -1;
            _dragFromEquipment = false;
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _codeField != null,
                    _codeField != null ? null : "Code field is null"),
                new("Stats bar exists", _statsBar != null,
                    _statsBar != null ? null : "Stats bar is null"),
                new("Main text panel has black background",
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black,
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black
                        ? null : "Main text panel background is not black")
            };
            return new ScenarioVerificationResult(checks);
        }

        private void ShowLootSelection(EquipmentItemDefinition[] options)
        {
            SetInputEnabled(false);

            _lootOverlay = new VisualElement();
            _lootOverlay.style.position = Position.Absolute;
            _lootOverlay.style.left = 0;
            _lootOverlay.style.top = 0;
            _lootOverlay.style.right = 0;
            _lootOverlay.style.bottom = 0;
            _lootOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _lootOverlay.style.justifyContent = Justify.Center;
            _lootOverlay.style.alignItems = Align.Center;
            _lootOverlay.pickingMode = PickingMode.Position;

            var title = new Label("VICTORY — Choose a Reward");
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
                var item = options[i];
                var cardIndex = i;

                var card = new VisualElement();
                card.style.width = 200;
                card.style.height = 250;
                card.style.marginLeft = 12;
                card.style.marginRight = 12;
                card.style.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
                card.style.borderTopWidth = 2;
                card.style.borderBottomWidth = 2;
                card.style.borderLeftWidth = 2;
                card.style.borderRightWidth = 2;
                card.style.borderTopColor = item.Color;
                card.style.borderBottomColor = item.Color;
                card.style.borderLeftColor = item.Color;
                card.style.borderRightColor = item.Color;
                card.style.borderTopLeftRadius = 8;
                card.style.borderTopRightRadius = 8;
                card.style.borderBottomLeftRadius = 8;
                card.style.borderBottomRightRadius = 8;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;
                card.style.justifyContent = Justify.FlexStart;
                card.style.alignItems = Align.Center;
                card.pickingMode = PickingMode.Position;

                var slotLabel = new Label(item.SlotType.ToString().ToUpperInvariant());
                slotLabel.style.fontSize = 14;
                slotLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                slotLabel.style.marginBottom = 8;
                slotLabel.pickingMode = PickingMode.Ignore;
                card.Add(slotLabel);

                var nameContainer = new VisualElement();
                nameContainer.style.width = 176;
                nameContainer.style.height = 120;
                nameContainer.style.justifyContent = Justify.Center;
                nameContainer.style.alignItems = Align.Center;
                nameContainer.style.marginBottom = 12;
                nameContainer.pickingMode = PickingMode.Ignore;
                var nameLayout = UnitTextLayout.Calculate(item.DisplayName, 176, 120);
                UnitTextLabels.AddTo(nameLayout, item.Color, nameContainer);
                card.Add(nameContainer);

                var statsText = BuildStatSummary(item.Stats);
                var statsLabel = new Label(statsText);
                statsLabel.style.fontSize = 14;
                statsLabel.style.color = Color.white;
                statsLabel.style.whiteSpace = WhiteSpace.Normal;
                statsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                statsLabel.pickingMode = PickingMode.Ignore;
                card.Add(statsLabel);

                card.RegisterCallback<PointerEnterEvent>(_ => ShowLootTooltip(item, card));
                card.RegisterCallback<PointerLeaveEvent>(_ => HideLootTooltip());
                card.RegisterCallback<ClickEvent>(_ => _lootRewardService.SelectReward(cardIndex));

                cardRow.Add(card);
            }

            RootVisualElement.Add(_lootOverlay);
        }

        private void HideLootSelection()
        {
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
        }

        private void ShowLootTooltip(EquipmentItemDefinition item, VisualElement card)
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

            var nameLabel = new Label(item.DisplayName);
            nameLabel.style.color = item.Color;
            nameLabel.style.fontSize = 22;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.marginBottom = 4;
            nameLabel.pickingMode = PickingMode.Ignore;
            _lootTooltip.Add(nameLabel);

            var slotLabel = new Label(item.SlotType.ToString());
            slotLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            slotLabel.style.fontSize = 16;
            slotLabel.style.marginBottom = 8;
            slotLabel.pickingMode = PickingMode.Ignore;
            _lootTooltip.Add(slotLabel);

            AddStatLine(item.Stats.MaxHealth, "Max Health");
            AddStatLine(item.Stats.PhysDefense, "Physical Defense");
            AddStatLine(item.Stats.MagicDefense, "Magic Defense");
            AddStatLine(item.Stats.Strength, "Strength");
            AddStatLine(item.Stats.MagicPower, "Magic Power");
            AddStatLine(item.Stats.Luck, "Luck");

            void AddStatLine(int value, string label)
            {
                if (value <= 0) return;
                var line = new Label($"+{value} {label}");
                line.style.color = new Color(0.3f, 1f, 0.3f);
                line.style.fontSize = 16;
                line.style.marginBottom = 2;
                line.pickingMode = PickingMode.Ignore;
                _lootTooltip.Add(line);
            }

            RootVisualElement.Add(_lootTooltip);

            var cardBound = card.worldBound;
            _lootTooltip.style.left = cardBound.x + cardBound.width + 12;
            _lootTooltip.style.top = cardBound.y;
        }

        private void HideLootTooltip()
        {
            _lootTooltip?.RemoveFromHierarchy();
            _lootTooltip = null;
        }

        private static string BuildStatSummary(StatBonus stats)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (stats.MaxHealth > 0) parts.Add($"+{stats.MaxHealth} HP");
            if (stats.PhysDefense > 0) parts.Add($"+{stats.PhysDefense} DEF");
            if (stats.MagicDefense > 0) parts.Add($"+{stats.MagicDefense} MDEF");
            if (stats.Strength > 0) parts.Add($"+{stats.Strength} STR");
            if (stats.MagicPower > 0) parts.Add($"+{stats.MagicPower} MAG");
            if (stats.Luck > 0) parts.Add($"+{stats.Luck} LCK");
            return string.Join(", ", parts);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _statusVisualService?.Dispose();
            _statusVisualService = null;
            _statusEffects = null;
            _animationService?.Dispose();
            (_passiveService as IDisposable)?.Dispose();
            (_combatLoop as IDisposable)?.Dispose();
            (_combatAI as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _eventBus?.ClearAllSubscriptions();
            _animationService = null;
            _passiveService = null;
            _combatLoop = null;
            _combatAI = null;
            _turnService = null;
            _encounterAdapter = null;
            _eventBus = null;
            _service = null;
            _unitService = null;
            _codeField = null;
            _linesContainer = null;
            _mainTextPanel = null;
            _statsBar = null;
            _slotVisual = null;
            _wordMatchService = null;
            _wordResolver = null;
            _previewService = null;
            _ammoPreviewService = null;
            _combatContext = null;
            _slotService = null;
            _entityStats = null;
            _weaponService = null;
            _weaponExecutor = null;
            _actionExecution = null;
            _ammoResolver = null;
            _ammoMatchService = null;
            _weaponSlot = null;
            _weaponDurabilityLabel = null;
            CancelDrag();
            (_lootRewardService as IDisposable)?.Dispose();
            _lootRewardService = null;
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
            _tooltipLayer = null;
            (_equipmentService as IDisposable)?.Dispose();
            (_inventoryService as IDisposable)?.Dispose();
            _equipmentService = null;
            _inventoryService = null;
            _itemRegistry = null;
            _leftBar = null;
            _rightBar = null;
            _allyRow = null;
            _hpBar = null;
            _hpLabel = null;
            _manaBar = null;
            _manaLabel = null;
            _manaCostOverlay = null;
        }

        private sealed class ScenarioAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
