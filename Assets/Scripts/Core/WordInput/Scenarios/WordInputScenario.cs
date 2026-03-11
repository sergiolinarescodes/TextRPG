using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Consumable;
using TextRPG.Core.Encounter;
using TextRPG.Core.CombatAI;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.Passive;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Run;
using TextRPG.Core.Scroll;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Testing;
using Unidad.Core.UI.Components;
using Unidad.Core.UI.TextAnimation.ElementAnimation;
using Unidad.Core.UI.Tooltip;
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
        private bool _isPreviewingManaCost;
        private ILootRewardService _lootRewardService;
        private VisualElement _lootOverlay;
        private VisualElement _lootTooltip;
        private int _highlightedSlotIndex = -1;
        private VisualElement _tooltipLayer;
        private ITooltipService _frameworkTooltipService;
        private IConsumableService _consumableService;
        private ConsumableActionExecutor _consumableExecutor;
        private DrunkLetterService _drunkLetterService;
        private VisualElement _consumableSlot;
        private Label _consumableDurabilityLabel;
        private IVisualElementScheduledItem _drunkWobbleSchedule;
        private EntityTooltipService _tooltipService;
        private IWordResolver _enemyResolver;
        private IActionRegistry _actionRegistry;

        private PlayerStatsBarVisual _playerStatsBar;

        // Run system fields
        private IRunService _runService;
        private Dictionary<string, EntityDefinition> _allUnits;
        private Dictionary<string, EventEncounterDefinition> _allEventEncounters;
        private IActionHandlerRegistry _handlerRegistry;
        private ActionHandlerContext _actionHandlerCtx;
        private ScenarioAnimationResolver _scenarioAnimResolver;
        private IEventEncounterService _eventEncounterService;
        private IEventEncounterLoopService _eventEncounterLoop;
        private ReactionService _reactionService;
        private EventEncounterContext _reactionContext;
        private Unidad.Core.Resource.IResourceService _resourceService;
        private Label _nodeProgressLabel;
        private GameMessageService _gameMessages;
        private Func<EntityId, Vector3> _positionProvider;
        private IWordCooldownService _wordCooldown;
        private IGiveValidator _giveValidator;
        private IWordTagResolver _wordTagResolver;
        private SpellWordResolver _spellResolver;
        private ISpellService _spellService;

        private bool _givePrefixDetected;

        private static readonly Color HighlightEnemy = new(1f, 0.3f, 0.3f, 0.4f);
        private static readonly Color HighlightSelf = new(0.3f, 1f, 0.3f, 0.4f);
        private static readonly Color GivePrefixColor = new(0.55f, 0.65f, 0.82f);

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
            // DrunkLetterService created early for WordInputService injection (playerId set below)
            var playerId = new EntityId("player");
            _drunkLetterService = new DrunkLetterService(_eventBus, playerId);
            _service = new WordInputService(_eventBus, _drunkLetterService);
            _unitService = new UnitService(_eventBus);
            _entityStats = new EntityStatsService(_eventBus);
            var wordActionData = WordActionDatabaseLoader.Load();
            _wordResolver = new FilteredWordResolver(wordActionData.Resolver, wordActionData.AmmoWordSet);
            _ammoResolver = wordActionData.AmmoResolver;
            _actionRegistry = wordActionData.ActionRegistry;
            _wordTagResolver = wordActionData.TagResolver;
            _wordMatchService = new WordMatchService(_wordResolver, _actionRegistry);
            _ammoMatchService = new WordMatchService(_ammoResolver, _actionRegistry);

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
            _playerId = playerId;
            PlayerDefaults.Register(_entityStats, _playerId);

            // Create player inventory
            _inventoryService.Create(_playerInventoryId, new InventoryDefinition(EquipmentConstants.InventorySlotCount));

            // Define inventory items from item registry
            foreach (var itemWord in _itemRegistry.Keys)
            {
                if (_itemRegistry.TryGet(itemWord, out var itemDef))
                    _inventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(new ItemId(itemWord), itemDef.DisplayName, 1));
            }

            // Item handler registered later (after _equipmentService is created)

            // Load all definitions from DB
            _allUnits = UnitDatabaseLoader.LoadAll();
            _allEventEncounters = LoadEventEncounters();

            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(_playerId);
            _encounterAdapter.SetEventBus(_eventBus);
            var enemyWordResolver = new EnemyWordResolver();
            _enemyResolver = enemyWordResolver;

            _combatContext.SetSourceEntity(_playerId);

            _previewService = new TargetingPreviewService(_wordResolver, _combatContext);
            _ammoPreviewService = new TargetingPreviewService(_ammoResolver, _combatContext);

            // Shared animation resolver for entire run
            _scenarioAnimResolver = new ScenarioAnimationResolver();

            // Spell system — run-lifetime
            _spellResolver = new SpellWordResolver();

            // Action execution — uses composite resolver (spell resolver first for priority)
            var compositeResolver = new CompositeWordResolver(_spellResolver, _wordResolver, _enemyResolver);
            _actionExecution = new ActionExecutionService(_eventBus, compositeResolver, handlerRegistry, _combatContext, _entityStats, statusEffects, _scenarioAnimResolver);

            // Rebuild match service with composite resolver so spell words are detected while typing
            _wordMatchService = new WordMatchService(compositeResolver, _actionRegistry);

            // Weapon executor
            _weaponExecutor = new WeaponActionExecutor(
                _eventBus, _weaponService, _ammoResolver, handlerRegistry, _combatContext, _scenarioAnimResolver);

            // Consumable service (must be created before executor)
            var consumableRegistry = ConsumableSystemInstaller.BuildConsumableRegistry(_itemRegistry);
            _consumableService = new ConsumableService(_eventBus, consumableRegistry);

            // Consumable executor
            _consumableExecutor = new ConsumableActionExecutor(
                _eventBus, _consumableService, _ammoResolver, handlerRegistry, _combatContext, _scenarioAnimResolver);

            // Action Animation service (created early so PassiveContext can reference it)
            _animationService = new ActionAnimationService(_eventBus, _scenarioAnimResolver, handlerRegistry, _entityStats);

            // Passive system
            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry();
            var targetResolver = new PassiveTargetResolver();
            var passiveContext = new PassiveContext(_entityStats, _slotService, _eventBus, _encounterAdapter,
                tagResolver: _wordTagResolver, animationService: _animationService);
            _passiveService = new PassiveService(_eventBus, triggerRegistry, effectRegistry, targetResolver, passiveContext, _allUnits);

            // Resource service — run-lifetime gold tracking
            _resourceService = new Unidad.Core.Resource.ResourceService(_eventBus);
            _resourceService.Define(
                EventEncounter.Reactions.Tags.ResourceIds.Gold,
                new Unidad.Core.Resource.ResourceDefinition(0, 0, 99999));

            // Reaction service — run-lifetime, enables tag reactions in both combat and events
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            _reactionContext = new EventEncounterContext(_entityStats, _slotService, _eventBus, _statusEffects, _resourceService,
                _inventoryService, _playerInventoryId, _itemRegistry);
            _reactionService = new ReactionService(_eventBus, outcomeRegistry, _reactionContext, tagReactions, _combatContext);

            // DrunkLetterService needs StatusEffectService reference
            _drunkLetterService.SetStatusEffects(_statusEffects);

            // Equipment service (needs passive service + consumable service)
            _equipmentService = new EquipmentService(_eventBus, _itemRegistry, _entityStats, _passiveService, _weaponService, _consumableService);

            // Register Item action handler (needs _equipmentService + _itemRegistry for auto-equip)
            _actionHandlerCtx = actionHandlerCtx;
            _handlerRegistry = handlerRegistry;
            handlerRegistry.Register("Item", new ItemActionHandler(actionHandlerCtx, _inventoryService, _playerInventoryId, _equipmentService, _itemRegistry));

            // Loot reward service (with scroll support)
            _lootRewardService = new LootRewardService(_eventBus, _itemRegistry, _inventoryService,
                _playerInventoryId, _playerId, _spellService, _wordResolver);

            _statusVisualService = new StatusEffectVisualService(_eventBus);

            var tickRunner = SceneRoot.AddComponent<TickRunner>();
            tickRunner.Initialize(new UnityTimeProvider(), new ITickable[] { _animationService });

            // Word cooldown — run-lifetime, resets per encounter
            _wordCooldown = new WordCooldownService();

            // Give validator — run-lifetime, checks inventory for giveable items or gold resource for Pay words
            _giveValidator = new GiveValidator(
                _wordTagResolver, _inventoryService, _playerInventoryId, _itemRegistry,
                _wordResolver, _resourceService);

            // Spell service — run-lifetime (persists learned spells across encounters)
            _spellService = new SpellService(_eventBus, _wordResolver, _wordCooldown, _spellResolver, _wordTagResolver);

            // Run service — manages node progression
            _runService = new RunService(_eventBus);

            // Generate and start run
            var runDef = RunMapGenerator.Generate(10, _allUnits, _allEventEncounters);

            // Subscribe to run events BEFORE starting
            _subscriptions.Add(_eventBus.Subscribe<RunNodeStartedEvent>(OnRunNodeStarted));
            _subscriptions.Add(_eventBus.Subscribe<RunNodeCompletedEvent>(OnRunNodeCompleted));
            _subscriptions.Add(_eventBus.Subscribe<RunCompletedEvent>(OnRunCompleted));
            _subscriptions.Add(_eventBus.Subscribe<EscapeAttemptedEvent>(evt =>
                Debug.Log($"[Run] Escape attempted — {(evt.Success ? "SUCCESS" : "FAILED")}")));
            _subscriptions.Add(_eventBus.Subscribe<InteractionMessageEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Message: {evt.Message}");
                var pos = _positionProvider?.Invoke(evt.SourceEntityId) ?? Vector3.zero;
                _gameMessages?.Spawn(new Vector2(pos.x, pos.y), evt.Message, new Color(1f, 0.9f, 0.5f));
            }));
            _subscriptions.Add(_eventBus.Subscribe<WordCooldownEvent>(evt =>
            {
                var msg = evt.Permanent
                    ? $"\"{evt.Word}\" is permanently exhausted!"
                    : $"\"{evt.Word}\" on cooldown ({evt.RemainingRounds} rounds)";
                Debug.Log($"[WordCooldown] {msg}");
                var pos = _positionProvider?.Invoke(_playerId) ?? Vector3.zero;
                _gameMessages?.Spawn(new Vector2(pos.x, pos.y), msg, new Color(1f, 0.4f, 0.4f));
            }));

            _runService.StartRun(runDef, _playerId);

            // --- Subscribe to weapon events ---
            _subscriptions.Add(_eventBus.Subscribe<WeaponEquippedEvent>(OnWeaponEquipped));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDurabilityChangedEvent>(OnWeaponDurabilityChanged));
            _subscriptions.Add(_eventBus.Subscribe<WeaponDestroyedEvent>(OnWeaponDestroyed));

            // --- Subscribe to consumable events ---
            _subscriptions.Add(_eventBus.Subscribe<ConsumableEquippedEvent>(OnConsumableEquipped));
            _subscriptions.Add(_eventBus.Subscribe<ConsumableDurabilityChangedEvent>(OnConsumableDurabilityChanged));
            _subscriptions.Add(_eventBus.Subscribe<ConsumableDestroyedEvent>(OnConsumableDestroyed));

            // --- Subscribe to drunk letter changes ---
            _subscriptions.Add(_eventBus.Subscribe<DrunkLettersChangedEvent>(_ => RefreshDrunkVisuals()));

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

            // Re-render slot cells when HP changes
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(evt =>
            {
                RefreshEntityCell(evt.EntityId);
            }));
            _subscriptions.Add(_eventBus.Subscribe<HealedEvent>(evt =>
            {
                RefreshEntityCell(evt.EntityId);
            }));
            _subscriptions.Add(_eventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (_encounterAdapter != null)
                    _encounterAdapter.MarkDead(evt.EntityId);
                if (evt.EntityId.Equals(_playerId))
                    RefreshEntityCell(evt.EntityId);
                else
                    _slotVisual?.PlayDeathAnimation(evt.EntityId);
            }));

            // Clear mana cost preview on mana change + refresh unit mana bars
            _subscriptions.Add(_eventBus.Subscribe<ManaChangedEvent>(evt =>
            {
                if (evt.EntityId.Equals(_playerId) && _isPreviewingManaCost)
                    ClearManaCostPreview();
                if (!evt.EntityId.Equals(_playerId))
                    RefreshEntityCell(evt.EntityId);
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
                if (_combatLoop != null && !_combatLoop.IsPlayerTurn)
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
                _playerStatsBar.HpLabel.text = "DEAD";
                _playerStatsBar.HpLabel.style.color = Color.red;
                Debug.Log("[Turn] GAME OVER — Player died!");
            }));

            // Register summoned units with the unit service for proper text rendering
            _subscriptions.Add(_eventBus.Subscribe<UnitSummonedEvent>(evt =>
            {
                var uid = new UnitId(evt.EntityId.Value);
                if (_unitService.TryGetUnit(uid, out _)) return;

                var word = evt.Word.ToLowerInvariant();
                if (_allUnits.TryGetValue(word, out var unitDef))
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
            }));

            // Slot visual removal for recruitment (not death — death has its own animation)
            _subscriptions.Add(_eventBus.Subscribe<SlotEntityRemovedEvent>(evt =>
            {
                if (_slotVisual == null) return;
                if (_entityStats.GetCurrentHealth(evt.EntityId) <= 0) return;
                _slotVisual.UnregisterEntity(evt.EntityId);
            }));

            // Loot reward events
            _subscriptions.Add(_eventBus.Subscribe<LootRewardOfferedEvent>(evt => ShowLootSelection(evt.Options)));
            _subscriptions.Add(_eventBus.Subscribe<LootRewardSelectedEvent>(_ => HideLootSelection()));

            // Spell learned event
            _subscriptions.Add(_eventBus.Subscribe<SpellLearnedEvent>(evt =>
            {
                Debug.Log($"[Scroll] Learned spell: {evt.ScrambledWord} (from {evt.OriginalWord}, cost={evt.ManaCost})");
                var pos = _positionProvider?.Invoke(_playerId) ?? Vector3.zero;
                _gameMessages?.Spawn(new Vector2(pos.x, pos.y),
                    $"Learned: {evt.ScrambledWord.ToUpperInvariant()}", ScrollDefinition.ScrollPurple);
            }));

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

            // Node progress label
            _nodeProgressLabel = new Label("Node 1/10 — Combat");
            _nodeProgressLabel.style.fontSize = 18;
            _nodeProgressLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            _nodeProgressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nodeProgressLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _nodeProgressLabel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            _nodeProgressLabel.style.paddingTop = 4;
            _nodeProgressLabel.style.paddingBottom = 4;
            root.Add(_nodeProgressLabel);

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
            _rightBar.SetSlotBackground(2, "ACCESSORY", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(3, "USE", SlotColors.Placeholder);
            _rightBar.SetSlotBackground(4, "WEAPON", SlotColors.Placeholder);

            // Set inventory slot placeholders on left bar
            for (int i = 0; i < EquipmentConstants.InventorySlotCount; i++)
                _leftBar.SetSlotBackground(i, "INVENTORY", SlotColors.Placeholder);

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

            // Stats bar (HP, Mana, Stats, Status Effects)
            _playerStatsBar = new PlayerStatsBarVisual(_eventBus, _entityStats, _statusEffects, _playerId, _resourceService);
            root.Add(_playerStatsBar.Build());

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            // Position provider
            Func<EntityId, Vector3> positionProvider = entityId =>
            {
                if (entityId.Equals(_playerId))
                {
                    var hpCenter = _playerStatsBar.HpBar.worldBound.center;
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
            _positionProvider = positionProvider;
            _animationService.Initialize(positionProvider, projectileOverlay);

            // Status effect visual overlays
            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);

            // Game message overlay (generic arc+bounce messages)
            var messageOverlay = GameMessageService.CreateOverlay();
            root.Add(messageOverlay);
            _gameMessages = new GameMessageService();
            _gameMessages.Initialize(messageOverlay);

            _tooltipLayer = new VisualElement { name = "tooltip-layer" };
            _tooltipLayer.style.position = Position.Absolute;
            _tooltipLayer.style.left = 0;
            _tooltipLayer.style.top = 0;
            _tooltipLayer.style.right = 0;
            _tooltipLayer.style.bottom = 0;
            _tooltipLayer.pickingMode = PickingMode.Ignore;
            root.Add(_tooltipLayer);

            _statusVisualService.Initialize(positionProvider, statusTextOverlay, _slotVisual);

            // Framework tooltip service
            var elementAnimator = new ElementAnimator();
            _frameworkTooltipService = new TooltipService(_eventBus, elementAnimator);
            _frameworkTooltipService.SetTooltipLayer(_tooltipLayer);

            // Entity tooltip service — handles hover tooltips for combat, equipment, and inventory slots
            _tooltipService = new EntityTooltipService(_eventBus);
            _tooltipService.Initialize(
                _slotVisual.GetAllSlotElements(),
                _rightBar.GetAllSlotElements(),
                _leftBar.GetAllSlotElements(),
                _frameworkTooltipService,
                _slotService, _statusEffects, _unitService, _passiveService,
                _encounterAdapter, _actionRegistry, _handlerRegistry, _enemyResolver,
                _ammoResolver, _weaponService, _consumableService, _equipmentService,
                _itemRegistry, _entityStats, _inventoryService, _playerInventoryId, _playerId);

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

            // Consumable slot — uses slot 3 of right bar
            _consumableSlot = _rightBar.GetSlotElement(3);
            _consumableSlot.pickingMode = PickingMode.Position;

            _consumableDurabilityLabel = new Label();
            _consumableDurabilityLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _consumableDurabilityLabel.style.fontSize = 20;
            _consumableDurabilityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _consumableDurabilityLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _consumableDurabilityLabel.style.position = Position.Absolute;
            _consumableDurabilityLabel.style.bottom = 2;
            _consumableDurabilityLabel.style.left = 4;

            _consumableSlot.RegisterCallback<ClickEvent>(_ => UseConsumable());

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
            (_weaponService != null && _weaponService.HasWeapon(_playerId)
            && _weaponService.IsAmmoForEquipped(_playerId, word))
            || (_consumableService != null && _consumableService.HasConsumable(_playerId)
            && _consumableService.IsAmmoForEquipped(_playerId, word));

        private void FireWeapon()
        {
            if (_combatLoop == null || !_combatLoop.FireWeapon()) return;

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

            // Use remapped text for display and matching (differs from newText when drunk)
            var displayText = _service.CurrentWord;

            RecalculateFontSize();

            // Update label text to show remapped characters immediately
            if (_drunkLetterService != null && _drunkLetterService.IsActive)
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < labels.Count && i < displayText.Length; i++)
                    labels[i].text = displayText[i].ToString();
            }

            // Detect "give " prefix
            var lowerText = displayText.ToLowerInvariant();
            bool isGivePrefix = lowerText.StartsWith("give ");
            var matchWord = isGivePrefix ? lowerText.Substring(5) : displayText;
            int prefixLen = isGivePrefix ? 5 : 0;

            // Wave animation on "give " when first detected
            if (isGivePrefix && !_givePrefixDetected)
            {
                _givePrefixDetected = true;
                var giveIndices = new List<int> { 0, 1, 2, 3, 4 };
                _codeField.PlayHighlightAnimation(giveIndices);
            }
            else if (!isGivePrefix)
            {
                _givePrefixDetected = false;
            }

            bool isAmmo = IsAmmoWord(matchWord);
            var matchService = isAmmo ? _ammoMatchService : _wordMatchService;
            var wasMatched = matchService.IsMatched;
            var colors = matchService.CheckMatch(matchWord);

            // Apply "give " prefix color
            if (isGivePrefix)
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < prefixLen && i < labels.Count; i++)
                    labels[i].style.color = GivePrefixColor;
            }

            if (colors.Count > 0)
            {
                var labels = _codeField.CharLabels;
                for (int i = 0; i < colors.Count && i + prefixLen < labels.Count; i++)
                    labels[i + prefixLen].style.color = colors[i].Color;

                if (!wasMatched)
                {
                    var indices = new List<int>();
                    for (int i = 0; i < colors.Count; i++)
                        indices.Add(i + prefixLen);
                    _codeField.PlayHighlightAnimation(indices);
                }

                var currentWord = lowerText;
                if (currentWord != _lastMatchedWord)
                {
                    _lastMatchedWord = currentWord;
                    ShowTargetingPreview(displayText);
                    if (!isAmmo)
                        ShowManaCostPreview(matchWord.ToLowerInvariant());
                    else
                        ClearManaCostPreview();
                }
            }
            else
            {
                var labels = _codeField.CharLabels;
                // Reset non-prefix chars to white
                for (int i = prefixLen; i < labels.Count; i++)
                    labels[i].style.color = Color.white;

                if (!isGivePrefix)
                {
                    for (int i = 0; i < labels.Count; i++)
                        labels[i].style.color = Color.white;
                }

                _lastMatchedWord = "";
                ClearTargetingPreview();
                ClearManaCostPreview();
            }

            // Apply drunk visual override — color letters that were remapped
            if (_drunkLetterService != null && _drunkLetterService.IsActive)
            {
                var drunkLabels = _codeField.CharLabels;
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
            ClearTargetingPreview();
            if (_previewService == null) return;

            var word = text.ToLowerInvariant();

            bool isGive = WordPrefixHelper.TryStripGivePrefix(ref word);
            if (isGive)
                _combatContext.SetTargetingInverted(true);
            else if (word.Length == 0) return;

            try
            {
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
            finally
            {
                if (isGive) _combatContext.SetTargetingInverted(false);
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
            // Use remapped word (after drunk letter scramble) so submit matches preview
            word ??= _service.CurrentWord?.Trim() ?? "";
            if (word.Length == 0) return;

            WordSubmitResult result;
            if (_combatLoop != null)
                result = _combatLoop.SubmitWord(word);
            else if (_eventEncounterLoop != null)
                result = _eventEncounterLoop.SubmitWord(word);
            else
                return;

            if (result == WordSubmitResult.InsufficientMana || result == WordSubmitResult.WordOnCooldown || result == WordSubmitResult.NoItemToGive)
            {
                PlayManaRejection();
                _codeField?.schedule.Execute(() => _codeField?.Focus());
                return;
            }
            if (result == WordSubmitResult.ReservedWord)
            {
                _service.Clear();
                _codeField.value = "";
                RecalculateFontSize();
                ClearTargetingPreview();
                ClearManaCostPreview();
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

        private void ShowManaCostPreview(string word)
        {
            if (_playerStatsBar.ManaBar == null || _playerStatsBar.ManaLabel == null || _playerStatsBar.ManaCostOverlay == null) return;

            if (!WordPrefixHelper.TryStripGivePrefix(ref word) && word.Length == 0) { ClearManaCostPreview(); return; }

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

            _playerStatsBar.ManaBar.Value = Mathf.Clamp01(previewRatio);

            float overlayLeft = Mathf.Max(0f, previewRatio);
            float overlayWidth = currentRatio - overlayLeft;
            _playerStatsBar.ManaCostOverlay.style.left = Length.Percent(overlayLeft * 100f);
            _playerStatsBar.ManaCostOverlay.style.width = Length.Percent(overlayWidth * 100f);
            _playerStatsBar.ManaCostOverlay.style.display = DisplayStyle.Flex;

            bool canAfford = previewMana >= 0;
            _playerStatsBar.ManaCostOverlay.style.backgroundColor = canAfford
                ? new Color(1f, 0.6f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);

            _playerStatsBar.ManaLabel.text = $"{previewMana}/{maxMana} (-{cost})";
            _playerStatsBar.ManaLabel.style.color = canAfford ? Color.white : Color.red;
        }

        private void ClearManaCostPreview()
        {
            if (!_isPreviewingManaCost) return;
            _isPreviewingManaCost = false;
            if (_playerStatsBar.ManaCostOverlay != null)
                _playerStatsBar.ManaCostOverlay.style.display = DisplayStyle.None;
            _playerStatsBar.UpdateManaBar();
            if (_playerStatsBar.ManaLabel != null)
                _playerStatsBar.ManaLabel.style.color = Color.white;
        }

        private void PlayManaRejection()
        {
            var labels = _codeField.CharLabels;
            var indices = new List<int>();
            for (int i = 0; i < labels.Count; i++)
                indices.Add(i);
            _codeField.PlayRejectionAnimation(indices);

            _playerStatsBar.ManaBar.SetVariant(ProgressVariant.Danger);
            _playerStatsBar.ManaBar.schedule.Execute(() => _playerStatsBar.ManaBar?.SetVariant(ProgressVariant.Info)).ExecuteLater(500);

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

        private void UseConsumable()
        {
            if (_combatLoop == null || !_combatLoop.UseConsumable()) return;

            _service.Clear();
            _codeField.value = "";
            ClearTargetingPreview();
            ClearManaCostPreview();
            _codeField?.schedule.Execute(() => _codeField?.Focus());
        }

        private void OnConsumableEquipped(ConsumableEquippedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Consumable equipped: {evt.Consumable.DisplayName} (dur={evt.Consumable.Durability})");
            _rightBar?.SetSlotContent(3, evt.Consumable.DisplayName, new Color(1f, 0.85f, 0.2f));
            _consumableDurabilityLabel.text = evt.Consumable.Durability.ToString();
            _consumableSlot?.Add(_consumableDurabilityLabel);
        }

        private void OnConsumableDurabilityChanged(ConsumableDurabilityChangedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Consumable durability: {evt.CurrentDurability}/{evt.MaxDurability}");
            _consumableDurabilityLabel.text = evt.CurrentDurability.ToString();
        }

        private void OnConsumableDestroyed(ConsumableDestroyedEvent evt)
        {
            if (!evt.Entity.Equals(_playerId)) return;
            Debug.Log($"[WordInputScenario] Consumable destroyed: {evt.Word}");
            _rightBar?.ClearSlotContent(3);
            if (_consumableDurabilityLabel?.parent != null)
                _consumableDurabilityLabel.RemoveFromHierarchy();
        }

        private void RefreshDrunkVisuals()
        {
            if (_codeField == null) return;
            var labels = _codeField.CharLabels;
            if (labels.Count == 0) return;

            // _codeField.value has original typed chars; remap and update labels
            var originalText = _codeField.value ?? "";
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
                // Reset transforms on all labels
                if (_codeField != null)
                {
                    var labels = _codeField.CharLabels;
                    for (int i = 0; i < labels.Count; i++)
                    {
                        labels[i].style.translate = new Translate(0, 0);
                        labels[i].RemoveFromClassList("drunk-letter");
                    }
                }
                return;
            }

            if (_drunkWobbleSchedule != null) return;

            _drunkWobbleSchedule = _codeField.schedule.Execute(() =>
            {
                if (_codeField == null || _drunkLetterService == null) return;
                var labels = _codeField.CharLabels;
                var text = _codeField.value ?? "";
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
            if (_encounterAdapter?.IsEncounterActive == true || _eventEncounterService?.IsEncounterActive == true) return;
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
            if (_encounterAdapter?.IsEncounterActive == true || _eventEncounterService?.IsEncounterActive == true) return;
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
                new("Stats bar exists", _playerStatsBar?.Root != null,
                    _playerStatsBar?.Root != null ? null : "Stats bar is null"),
                new("Main text panel has black background",
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black,
                    _mainTextPanel != null && _mainTextPanel.resolvedStyle.backgroundColor == Color.black
                        ? null : "Main text panel background is not black")
            };
            return new ScenarioVerificationResult(checks);
        }

        private void ShowLootSelection(LootRewardOption[] options)
        {
            SetInputEnabled(false);

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

            RootVisualElement.Add(_lootOverlay);
        }

        private void HideLootSelection()
        {
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
            ClearEquipmentSlotHighlight();
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

                var spellLabel = new Label($"Spell: {scroll.OriginalWord}");
                spellLabel.style.color = Color.white;
                spellLabel.style.fontSize = 14;
                spellLabel.style.marginTop = 4;
                _lootTooltip.Add(spellLabel);

                var costLabel = new Label($"Mana cost: {scroll.ManaCost}");
                costLabel.style.color = new Color(0.4f, 0.7f, 1f);
                costLabel.style.fontSize = 14;
                costLabel.style.marginTop = 2;
                _lootTooltip.Add(costLabel);

                var cdLabel = new Label("Cooldown: 2 rounds (fixed)");
                cdLabel.style.color = new Color(1f, 0.8f, 0.4f);
                cdLabel.style.fontSize = 14;
                cdLabel.style.marginTop = 2;
                _lootTooltip.Add(cdLabel);

                var typeLabel = new Label("MagicDamage");
                typeLabel.style.color = new Color(0.8f, 0.5f, 1f);
                typeLabel.style.fontSize = 14;
                typeLabel.style.marginTop = 2;
                _lootTooltip.Add(typeLabel);
            }
            else
            {
                var item = option.Equipment;
                _lootTooltip.Add(TooltipContentBuilder.BuildHeader(item.DisplayName, item.Color, item.SlotType.ToString()));
                _lootTooltip.Add(TooltipContentBuilder.BuildArmorContent(item));
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

        // --- Run system methods ---

        private void OnRunNodeStarted(RunNodeStartedEvent evt)
        {
            var node = _runService.CurrentRun.Nodes[evt.NodeIndex];
            if (node.CombatEncounter != null)
                StartCombatNode(node.CombatEncounter);
            else if (node.EventEncounter != null)
                StartEventNode(node.EventEncounter);
            UpdateNodeProgressLabel(evt.NodeIndex, evt.NodeType);
            Debug.Log($"[Run] Node {evt.NodeIndex + 1}/{_runService.CurrentRun.Nodes.Length} started: {evt.NodeType} — {evt.EncounterId}");
        }

        private void OnRunNodeCompleted(RunNodeCompletedEvent evt)
        {
            CleanupCurrentNode();
            Debug.Log($"[Run] Node {evt.NodeIndex + 1} completed (victory={evt.Victory})");
        }

        private void OnRunCompleted(RunCompletedEvent evt)
        {
            Debug.Log($"[Run] Run {(evt.Victory ? "VICTORY" : "DEFEAT")}!");
            if (evt.Victory)
                ShowRunCompleteOverlay();
        }

        private void StartCombatNode(EncounterDefinition encounter)
        {
            // Clear and repopulate enemy word resolver (shared with CompositeWordResolver)
            (_enemyResolver as EnemyWordResolver)?.Clear();

            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(_playerId);
            _encounterAdapter.SetEventBus(_eventBus);

            var enemyIds = new List<EntityId>();
            for (int i = 0; i < encounter.Enemies.Length && i < 3; i++)
            {
                var def = encounter.Enemies[i];
                var unitId = $"{def.Name.ToLowerInvariant()}_{i}";
                var entityId = new EntityId(unitId);

                _entityStats.RegisterEntity(entityId, def.MaxHealth, def.Strength, def.MagicPower,
                    def.PhysicalDefense, def.MagicDefense, def.Luck);
                _unitService.Register(new UnitId(unitId),
                    new UnitDefinition(new UnitId(unitId), def.Name,
                        def.MaxHealth, def.Strength, def.PhysicalDefense, def.Luck, def.Color));
                _slotService.RegisterEnemy(entityId, i);
                enemyIds.Add(entityId);
                _encounterAdapter.RegisterEnemy(entityId, def);

                // Find matching unit key for word registration
                var matchKey = _allUnits.FirstOrDefault(kvp =>
                    kvp.Value.Name == def.Name).Key;
                if (matchKey != null)
                    UnitDatabaseLoader.RegisterUnitWords((_enemyResolver as EnemyWordResolver), matchKey);
            }
            _encounterAdapter.Activate();

            _combatContext.SetEnemies(enemyIds.ToArray());
            _combatContext.SetAllies(Array.Empty<EntityId>());

            // Scorer registry
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(_statusEffects);

            // CombatAI service
            _combatAI = new CombatAIService(_eventBus, _encounterAdapter, _entityStats,
                _turnService, _slotService, _combatContext, _actionExecution, scorers,
                (_enemyResolver as EnemyWordResolver), _allUnits, statusEffects: _statusEffects);

            // Turn order: player first, then enemies
            var turnOrder = new List<EntityId> { _playerId };
            turnOrder.AddRange(enemyIds);
            _turnService.SetTurnOrder(turnOrder);

            // CombatLoop with RunService as reserved word handler
            _combatLoop = new CombatLoopService(
                _eventBus, _turnService, _entityStats, _wordResolver, _weaponService, _playerId,
                _consumableService, (IReservedWordHandler)_runService, _combatContext, _wordCooldown, _giveValidator);
            _combatLoop.Start();

            // Register passives and tags for enemies
            for (int i = 0; i < encounter.Enemies.Length && i < 3; i++)
            {
                var def = encounter.Enemies[i];
                var unitId = $"{def.Name.ToLowerInvariant()}_{i}";
                var eid = new EntityId(unitId);
                if (def.Passives != null)
                    _passiveService.RegisterPassives(eid, def.Passives);
                if (def.Tags != null && def.Tags.Length > 0)
                    _reactionService.RegisterReactions(eid, null, def.Tags);
            }

            // Recruitment: unregister enemy when recruited
            _subscriptions.Add(_eventBus.Subscribe<EntityRecruitedEvent>(evt =>
            {
                _encounterAdapter.UnregisterEnemy(evt.EntityId);
            }));

            // Update slot visual
            // Slot visual refreshes via SlotEntityRegisteredEvent/RemovedEvent subscriptions
            SetInputEnabled(true);
        }

        private void StartEventNode(EventEncounterDefinition encounter)
        {
            // Use run-lifetime reaction service for event encounters
            _eventEncounterService = new EventEncounterService(
                _eventBus, _entityStats, _slotService, _combatContext, _reactionService);
            _reactionContext.EncounterService = _eventEncounterService;
            _tooltipService?.SetEventEncounterService(_eventEncounterService);

            // Register interactable UnitDefinitions for rendering
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");
                var uid = new UnitId(entityId.Value);
                _unitService.Register(uid,
                    new UnitDefinition(uid, def.Name, def.MaxHealth, 0, 0, 0, def.Color));
            }

            // Start encounter
            _eventEncounterService.StartEncounter(encounter, _playerId);

            // Register interactable passives
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var entityId = _eventEncounterService.InteractableEntities[i];
                    _passiveService.RegisterPassives(entityId, def.Passives);
                }
            }

            // Event encounter loop with RunService as reserved word handler
            _eventEncounterLoop = new EventEncounterLoopService(
                _eventBus, _entityStats, _wordResolver, _eventEncounterService, _playerId,
                (IReservedWordHandler)_runService, _combatContext, _wordCooldown, _giveValidator,
                maxInteractions: 2);
            _eventEncounterLoop.Start();

            // Update slot visual
            // Slot visual refreshes via SlotEntityRegisteredEvent/RemovedEvent subscriptions
            SetInputEnabled(true);
        }

        private void CleanupCurrentNode()
        {
            // Dispose per-encounter combat services
            (_combatLoop as IDisposable)?.Dispose();
            _combatLoop = null;
            (_combatAI as IDisposable)?.Dispose();
            _combatAI = null;

            // Dispose per-encounter event services
            (_eventEncounterLoop as IDisposable)?.Dispose();
            _eventEncounterLoop = null;
            if (_eventEncounterService?.IsEncounterActive == true)
                _eventEncounterService.EndEncounter();
            (_eventEncounterService as IDisposable)?.Dispose();
            _eventEncounterService = null;

            // Clear reactions (tags registered during combat or event encounters)
            _reactionService?.ClearReactions();

            // Clear encounter adapter
            if (_encounterAdapter != null)
            {
                _encounterAdapter.EndEncounter();
                _encounterAdapter = null;
            }

            // Clear enemy entities from slots
            _slotService.Initialize();

            // Clear enemy resolver (reuse same instance — tooltip service holds a reference)
            (_enemyResolver as EnemyWordResolver)?.Clear();

            // Reset combat context enemies
            _combatContext.SetEnemies(Array.Empty<EntityId>());
            _combatContext.SetAllies(Array.Empty<EntityId>());

            // Reset word cooldowns per encounter
            _wordCooldown?.Reset();

            // Update slot visual
            // Slot visual refreshes via SlotEntityRegisteredEvent/RemovedEvent subscriptions
        }

        private void UpdateNodeProgressLabel(int nodeIndex, RunNodeType nodeType)
        {
            if (_nodeProgressLabel == null) return;
            var total = _runService.CurrentRun.Nodes.Length;
            _nodeProgressLabel.text = $"Node {nodeIndex + 1}/{total} — {nodeType}";
        }

        private void ShowRunCompleteOverlay()
        {
            SetInputEnabled(false);

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.pickingMode = PickingMode.Position;

            var title = new Label("RUN COMPLETE");
            title.style.fontSize = 48;
            title.style.color = new Color(1f, 0.85f, 0.2f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.pickingMode = PickingMode.Ignore;
            overlay.Add(title);

            var subtitle = new Label("All encounters cleared!");
            subtitle.style.fontSize = 24;
            subtitle.style.color = Color.white;
            subtitle.style.marginTop = 16;
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitle.pickingMode = PickingMode.Ignore;
            overlay.Add(subtitle);

            RootVisualElement.Add(overlay);
        }

        private static Dictionary<string, EventEncounterDefinition> LoadEventEncounters()
        {
            var registry = new EventEncounterProviderRegistry();
            EventEncounterDatabaseLoader.LoadIntoRegistry(registry);

            var result = new Dictionary<string, EventEncounterDefinition>();
            foreach (var key in registry.Keys)
            {
                if (registry.TryGet(key, out var provider))
                    result[key] = provider.CreateDefinition();
            }
            return result;
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _tooltipService?.Dispose();
            _tooltipService = null;
            (_frameworkTooltipService as IDisposable)?.Dispose();
            _frameworkTooltipService = null;
            _gameMessages?.Dispose();
            _gameMessages = null;
            _positionProvider = null;
            _statusVisualService?.Dispose();
            _statusVisualService = null;
            _statusEffects = null;
            _animationService?.Dispose();
            (_passiveService as IDisposable)?.Dispose();
            (_combatLoop as IDisposable)?.Dispose();
            (_combatAI as IDisposable)?.Dispose();
            (_eventEncounterLoop as IDisposable)?.Dispose();
            (_eventEncounterService as IDisposable)?.Dispose();
            (_runService as IDisposable)?.Dispose();
            (_reactionService as IDisposable)?.Dispose();
            (_resourceService as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _eventBus?.ClearAllSubscriptions();
            _animationService = null;
            _passiveService = null;
            _reactionService = null;
            _reactionContext = null;
            _resourceService = null;
            _combatLoop = null;
            _combatAI = null;
            _eventEncounterLoop = null;
            _eventEncounterService = null;
            _runService = null;
            _turnService = null;
            _encounterAdapter = null;
            _eventBus = null;
            _service = null;
            _unitService = null;
            _codeField = null;
            _linesContainer = null;
            _mainTextPanel = null;
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
            _drunkWobbleSchedule?.Pause();
            _drunkWobbleSchedule = null;
            (_drunkLetterService as IDisposable)?.Dispose();
            _drunkLetterService = null;
            (_consumableService as IDisposable)?.Dispose();
            (_consumableExecutor as IDisposable)?.Dispose();
            _consumableService = null;
            _consumableExecutor = null;
            _consumableSlot = null;
            _consumableDurabilityLabel = null;
            CancelDrag();
            (_lootRewardService as IDisposable)?.Dispose();
            _lootRewardService = null;
            (_spellService as IDisposable)?.Dispose();
            _spellService = null;
            _spellResolver = null;
            _lootOverlay?.RemoveFromHierarchy();
            _lootOverlay = null;
            HideLootTooltip();
            _tooltipLayer = null;
            _enemyResolver = null;
            _actionRegistry = null;
            _handlerRegistry = null;
            _actionHandlerCtx = null;
            _scenarioAnimResolver = null;
            _allUnits = null;
            _allEventEncounters = null;
            _nodeProgressLabel = null;
            (_equipmentService as IDisposable)?.Dispose();
            (_inventoryService as IDisposable)?.Dispose();
            _equipmentService = null;
            _inventoryService = null;
            _itemRegistry = null;
            _giveValidator = null;
            _wordTagResolver = null;
            _leftBar = null;
            _rightBar = null;
            _allyRow = null;
            _playerStatsBar?.Dispose();
            _playerStatsBar = null;
        }

        private sealed class ScenarioAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
