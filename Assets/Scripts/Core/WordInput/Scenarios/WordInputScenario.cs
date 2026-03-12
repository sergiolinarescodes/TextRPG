using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.Experience;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.Passive;
using TextRPG.Core.PlayerClass;
using TextRPG.Core.Run;
using TextRPG.Core.Scroll;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
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

        private static readonly ScenarioParameter ClassParam = new(
            "playerClass", "Player Class (0=Mage, 1=Warrior, 2=Merchant, 3=Rogue)", typeof(int), 0, 0, 3);

        private RunSession _session;
        private CombatEncounterScope _combatScope;
        private EventEncounterScope _eventScope;

        // Controllers
        private WordInputController _wordInput;
        private EquipmentVisualController _equipment;
        private LootOverlayController _loot;
        private CombatVisualController _combat;
        private GameMessageController _messages;
        private ExperienceVisualController _experience;
        private PlayerStatsBarVisual _playerStatsBar;
        private CombatSlotVisual _slotVisual;
        private LetterChallenge.LetterChallengeVisual _letterChallenge;

        // Cross-cutting
        private EntityTooltipService _tooltipService;
        private ITooltipService _frameworkTooltipService;
        private StatusEffectVisualService _statusVisualService;
        private Func<EntityId, Vector3> _positionProvider;
        private Label _nodeProgressLabel;
        private ScenarioEncounterAdapter _encounterAdapter;
        private IEventEncounterService _eventEncounterService;

        private readonly List<IDisposable> _subscriptions = new();

        public WordInputScenario() : base(new TestScenarioDefinition(
            "word-input",
            "Word Input (Live)",
            "Full-screen word input with auto-scaling text, vibration animation, " +
            "slot-based combat, and stats bar. Type a word and press Enter to submit.",
            new[] { VibrationAmplitudeParam, FontScaleFactorParam, ClassParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
            var fontScaleFactor = ResolveParam<float>(overrides, "fontScaleFactor");
            var classIndex = ResolveParam<int>(overrides, "playerClass");
            var selectedClass = (TextRPG.Core.PlayerClass.PlayerClass)classIndex;

            // --- Create run session (all run-lifetime services) ---
            var playerId = new EntityId("player");
            _session = RunSessionFactory.Create(playerId, SceneRoot, new ScenarioAnimationResolver(), selectedClass);
            var s = _session;

            // --- Build UI layout ---
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

            _slotVisual = new CombatSlotVisual(s.UnitService, s.EntityStats, s.SlotService);
            _slotVisual.BuildEnemyRow(enemyRow);

            // Middle area: left bar | text panel | right bar
            var middleArea = new VisualElement();
            middleArea.style.flexDirection = FlexDirection.Row;
            middleArea.style.flexGrow = 1;

            // Framework tooltip service (created early so it can be passed to stats bar)
            var elementAnimator = new ElementAnimator();
            _frameworkTooltipService = new TooltipService(s.EventBus, elementAnimator);

            // Stats bar (created early so WordInputController can reference it for mana preview)
            _playerStatsBar = new PlayerStatsBarVisual(s.EventBus, s.EntityStats, s.StatusEffects, playerId,
                s.ResourceService, s.ClassService, _frameworkTooltipService);

            // Equipment controller (builds left + right bars)
            _equipment = new EquipmentVisualController(s.EventBus, s.EquipmentService,
                s.InventoryService, s.ItemRegistry, s.WeaponService, s.ConsumableService,
                s.PlayerInventoryId, playerId, root);
            _equipment.BuildBars(middleArea);

            // Word input controller (builds main text panel)
            _wordInput = new WordInputController(s.EventBus, s.WordInputService, s.DrunkLetterService,
                s.WordMatchService, s.AmmoMatchService, s.WordResolver, s.CombatContext,
                s.PreviewService, s.AmmoPreviewService, s.WeaponService, s.ConsumableService,
                s.LetterReserve, _playerStatsBar, _slotVisual, playerId, fontScaleFactor);

            var mainTextPanel = _wordInput.BuildInputArea(vibrationAmplitude);
            middleArea.Insert(1, mainTextPanel); // between left and right bars

            // Wire weapon/consumable click actions
            _equipment.FireWeaponAction = () => _wordInput.FireWeapon();
            _equipment.UseConsumableAction = () => _wordInput.UseConsumable();

            // Letter challenge visual (bottom-right corner)
            _letterChallenge = new LetterChallenge.LetterChallengeVisual(s.EventBus, root, playerId);

            // Ally row
            var allyRow = new VisualElement();
            allyRow.pickingMode = PickingMode.Ignore;
            allyRow.style.position = Position.Absolute;
            allyRow.style.bottom = 10;
            allyRow.style.left = 0;
            allyRow.style.right = 0;
            allyRow.style.flexDirection = FlexDirection.Row;
            allyRow.style.justifyContent = Justify.Center;
            allyRow.style.alignItems = Align.Center;
            allyRow.style.height = 100;
            allyRow.style.paddingTop = 4;
            allyRow.style.paddingBottom = 4;
            mainTextPanel.Add(allyRow);
            _slotVisual.BuildAllyRow(allyRow);

            root.Add(middleArea);

            // Stats bar visual (Build creates the UI elements)
            root.Add(_playerStatsBar.Build());

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            // Position provider
            _positionProvider = entityId =>
            {
                if (entityId.Equals(playerId))
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
            s.AnimationService.Initialize(_positionProvider, projectileOverlay);

            // Status effect visual overlays
            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);
            _statusVisualService = s.StatusVisualService;
            _statusVisualService.Initialize(_positionProvider, statusTextOverlay, _slotVisual);

            // Game message controller
            _messages = new GameMessageController(s.EventBus, _positionProvider, playerId);
            root.Add(_messages.CreateOverlay());

            // Experience visual controller
            _experience = new ExperienceVisualController(s.EventBus, s.ExperienceService,
                _playerStatsBar, _positionProvider);
            root.Add(_experience.CreateOverlay());

            // Combat visual controller
            _combat = new CombatVisualController(s.EventBus, s.EntityStats, s.UnitService,
                _slotVisual, s.AllUnits, playerId);

            // Tooltip layer
            var tooltipLayer = new VisualElement { name = "tooltip-layer" };
            tooltipLayer.style.position = Position.Absolute;
            tooltipLayer.style.left = 0;
            tooltipLayer.style.top = 0;
            tooltipLayer.style.right = 0;
            tooltipLayer.style.bottom = 0;
            tooltipLayer.pickingMode = PickingMode.Ignore;
            root.Add(tooltipLayer);

            // Set tooltip layer (service was created earlier for stats bar)
            _frameworkTooltipService.SetTooltipLayer(tooltipLayer);

            // Entity tooltip service
            _tooltipService = new EntityTooltipService(s.EventBus);
            _tooltipService.Initialize(
                _slotVisual.GetAllSlotElements(),
                _equipment.RightBar.GetAllSlotElements(),
                _equipment.LeftBar.GetAllSlotElements(),
                _frameworkTooltipService,
                s.SlotService, s.StatusEffects, s.UnitService, s.PassiveService,
                _encounterAdapter, s.ActionRegistry, s.HandlerRegistry, s.EnemyResolver,
                s.AmmoResolver, s.WeaponService, s.ConsumableService, s.EquipmentService,
                s.ItemRegistry, s.EntityStats, s.InventoryService, s.PlayerInventoryId, playerId);

            // Loot overlay controller
            _loot = new LootOverlayController(s.EventBus, s.LootRewardService,
                _equipment.RightBar, root, enabled => _wordInput.SetInputEnabled(enabled));

            // --- Subscribe to run lifecycle events ---
            _subscriptions.Add(s.EventBus.Subscribe<RunNodeStartedEvent>(OnRunNodeStarted));
            _subscriptions.Add(s.EventBus.Subscribe<RunNodeCompletedEvent>(OnRunNodeCompleted));
            _subscriptions.Add(s.EventBus.Subscribe<RunCompletedEvent>(OnRunCompleted));
            _subscriptions.Add(s.EventBus.Subscribe<EscapeAttemptedEvent>(evt =>
                Debug.Log($"[Run] Escape attempted — {(evt.Success ? "SUCCESS" : "FAILED")}")));
            _subscriptions.Add(s.EventBus.Subscribe<GameOverEvent>(_ =>
            {
                _wordInput.SetInputEnabled(false);
                _playerStatsBar.HpLabel.text = "DEAD";
                _playerStatsBar.HpLabel.style.color = Color.red;
                Debug.Log("[Turn] GAME OVER — Player died!");
            }));

            // Debug log subscriptions
            _subscriptions.Add(s.EventBus.Subscribe<WordSubmittedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var actions = s.WordResolver.Resolve(word);
                var meta = s.WordResolver.GetStats(word);
                if (actions.Count > 0)
                    Debug.Log($"[WordInputScenario] \"{evt.Word}\" → {meta.Target} cost={meta.Cost} | {string.Join(", ", actions.Select(a => $"{a.ActionId}({a.Value})"))}");
                else
                    Debug.Log($"[WordInputScenario] \"{evt.Word}\" → no actions");
            }));
            _subscriptions.Add(s.EventBus.Subscribe<WordClearedEvent>(_ =>
                Debug.Log("[WordInputScenario] Word cleared")));
            _subscriptions.Add(s.EventBus.Subscribe<WordRejectedEvent>(evt =>
                Debug.Log($"[WordInputScenario] Insufficient mana for \"{evt.Word}\" (cost={evt.ManaCost})")));
            _subscriptions.Add(s.EventBus.Subscribe<PassiveTriggeredEvent>(evt =>
                Debug.Log($"[Passive] {evt.TriggerId}+{evt.EffectId} from {evt.SourceEntity.Value} → value={evt.Value} affected={evt.AffectedEntity?.Value ?? "none"}")));
            _subscriptions.Add(s.EventBus.Subscribe<ActionHandlerExecutedEvent>(evt =>
            {
                if (_combatScope?.CombatLoop != null && !_combatScope.CombatLoop.IsPlayerTurn)
                    Debug.Log($"[Turn:Enemy] {evt.ActionId}({evt.Value}) → {evt.Targets.Count} target(s)");
            }));
            _subscriptions.Add(s.EventBus.Subscribe<InteractionMessageEvent>(evt =>
                Debug.Log($"[EventEncounter] Message: {evt.Message}")));
            _subscriptions.Add(s.EventBus.Subscribe<WordCooldownEvent>(evt =>
            {
                var msg = evt.Permanent ? $"\"{evt.Word}\" is permanently exhausted!" : $"\"{evt.Word}\" on cooldown ({evt.RemainingRounds} rounds)";
                Debug.Log($"[WordCooldown] {msg}");
            }));
            _subscriptions.Add(s.EventBus.Subscribe<SpellLearnedEvent>(evt =>
                Debug.Log($"[Scroll] Learned spell: {evt.ScrambledWord} (from {evt.OriginalWord}, cost={evt.ManaCost})")));
            _subscriptions.Add(s.EventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
                Debug.Log($"[Turn] === Player's turn (Turn #{evt.TurnNumber}, Round #{evt.RoundNumber}) ===")));
            _subscriptions.Add(s.EventBus.Subscribe<PlayerTurnEndedEvent>(_ =>
                Debug.Log("[Turn] Player's turn ended")));

            // Generate and start run
            var runDef = RunMapGenerator.Generate(10, s.AllUnits, s.AllEventEncounters);
            s.RunService.StartRun(runDef, playerId);

            Debug.Log($"[WordInputScenario] Started — vibration={vibrationAmplitude}, fontScale={fontScaleFactor}");
        }

        // --- Run lifecycle ---

        private void OnRunNodeStarted(RunNodeStartedEvent evt)
        {
            var node = _session.RunService.CurrentRun.Nodes[evt.NodeIndex];
            if (node.CombatEncounter != null)
                StartCombatNode(node.CombatEncounter);
            else if (node.EventEncounter != null)
                StartEventNode(node.EventEncounter);
            UpdateNodeProgressLabel(evt.NodeIndex, evt.NodeType);
            Debug.Log($"[Run] Node {evt.NodeIndex + 1}/{_session.RunService.CurrentRun.Nodes.Length} started: {evt.NodeType} — {evt.EncounterId}");
        }

        private void OnRunNodeCompleted(RunNodeCompletedEvent evt)
        {
            CleanupCurrentNode();
            Debug.Log($"[Run] Node {evt.NodeIndex + 1} completed (victory={evt.Victory})");
        }

        private void OnRunCompleted(RunCompletedEvent evt)
        {
            Debug.Log($"[Run] Run {(evt.Victory ? "VICTORY" : "DEFEAT")}!");
            if (evt.Victory) ShowRunCompleteOverlay();
        }

        private void StartCombatNode(EncounterDefinition encounter)
        {
            _combatScope = _session.StartCombat(encounter);
            _encounterAdapter = _combatScope.EncounterAdapter;
            _tooltipService?.SetEncounterService(_encounterAdapter);
            _wordInput.SetCombatLoop(_combatScope.CombatLoop);
            _wordInput.SetInputEnabled(true);
        }

        private void StartEventNode(EventEncounterDefinition encounter)
        {
            _eventScope = _session.StartEvent(encounter);
            _eventEncounterService = _eventScope.EncounterService;
            _wordInput.SetEventLoop(_eventScope.LoopService);
            _tooltipService?.SetEventEncounterService(_eventEncounterService);
            _wordInput.SetInputEnabled(true);
        }

        private void CleanupCurrentNode()
        {
            _combatScope?.Dispose();
            _combatScope = null;
            _eventScope?.Dispose();
            _eventScope = null;
            _session.CleanupCurrentEncounter();

            _encounterAdapter = null;
            _eventEncounterService = null;
            _tooltipService?.SetEncounterService(null);
            _tooltipService?.SetEventEncounterService(null);
            _wordInput.SetCombatLoop(null);
            _wordInput.SetEventLoop(null);
        }

        private void UpdateNodeProgressLabel(int nodeIndex, RunNodeType nodeType)
        {
            if (_nodeProgressLabel == null) return;
            var total = _session.RunService.CurrentRun.Nodes.Length;
            _nodeProgressLabel.text = $"Node {nodeIndex + 1}/{total} — {nodeType}";
        }

        private void ShowRunCompleteOverlay()
        {
            _wordInput.SetInputEnabled(false);

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

        // --- Verification & Cleanup ---

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _wordInput?.CodeField != null,
                    _wordInput?.CodeField != null ? null : "Code field is null"),
                new("Stats bar exists", _playerStatsBar?.Root != null,
                    _playerStatsBar?.Root != null ? null : "Stats bar is null"),
                new("Main text panel has black background",
                    _wordInput?.MainTextPanel != null && _wordInput.MainTextPanel.resolvedStyle.backgroundColor == Color.black,
                    _wordInput?.MainTextPanel != null && _wordInput.MainTextPanel.resolvedStyle.backgroundColor == Color.black
                        ? null : "Main text panel background is not black")
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _tooltipService?.Dispose();
            _tooltipService = null;
            (_frameworkTooltipService as IDisposable)?.Dispose();
            _frameworkTooltipService = null;
            _positionProvider = null;

            _loot?.Dispose();
            _loot = null;
            _experience?.Dispose();
            _experience = null;
            _messages?.Dispose();
            _messages = null;
            _combat?.Dispose();
            _combat = null;
            _letterChallenge?.Dispose();
            _equipment?.Dispose();
            _equipment = null;
            _wordInput?.Dispose();
            _wordInput = null;
            _playerStatsBar?.Dispose();
            _playerStatsBar = null;

            _combatScope?.Dispose();
            _combatScope = null;
            _eventScope?.Dispose();
            _eventScope = null;
            _session?.Dispose();
            _session = null;

            _slotVisual = null;
            _nodeProgressLabel = null;
            _encounterAdapter = null;
            _eventEncounterService = null;
        }

        private sealed class ScenarioAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
