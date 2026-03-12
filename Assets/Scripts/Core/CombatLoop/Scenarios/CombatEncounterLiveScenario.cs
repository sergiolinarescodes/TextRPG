using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using Unidad.Core.UI.TextAnimation.ElementAnimation;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.CombatLoop.Scenarios
{
    internal sealed class CombatEncounterLiveScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter VibrationAmplitudeParam = new(
            "vibrationAmplitude", "Vibration Amplitude", typeof(float), 3.0f, 0f, 10f);

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private RunSession _session;
        private EncounterService _encounterService;
        private ICombatLoopService _combatLoop;
        private ICombatAIService _combatAI;

        private WordInputController _wordInput;
        private EquipmentVisualController _equipment;
        private LootOverlayController _loot;
        private CombatVisualController _combat;
        private GameMessageController _messages;
        private PlayerStatsBarVisual _playerStatsBar;
        private CombatSlotVisual _slotVisual;
        private EntityTooltipService _tooltipService;
        private ITooltipService _frameworkTooltipService;

        private readonly List<IDisposable> _subscriptions = new();

        public CombatEncounterLiveScenario() : base(new TestScenarioDefinition(
            "combat-encounter-live",
            "Combat Encounter (Live)",
            "Live interactive combat using the real EncounterService — enemies auto-spawned, " +
            "turns cycle, AI takes actions, full animation pipeline. Type words and press Enter.",
            new[] { VibrationAmplitudeParam, FontScaleFactorParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
            var fontScaleFactor = ResolveParam<float>(overrides, "fontScaleFactor");

            var playerId = new EntityId("player");
            _session = RunSessionFactory.Create(playerId, SceneRoot, new LiveAnimationResolver());
            var s = _session;

            // Real EncounterService (uses its own enemy spawning)
            var enemyResolver = s.EnemyResolver as EnemyWordResolver;
            _encounterService = new EncounterService(
                s.EventBus, s.EntityStats, s.TurnService, s.SlotService,
                s.CombatContext, enemyResolver);

            // Register units from EnemySpawnedEvent
            _subscriptions.Add(s.EventBus.Subscribe<EnemySpawnedEvent>(evt =>
            {
                var unitKey = evt.EnemyName.ToLowerInvariant();
                var uid = new UnitId(evt.EntityId.Value);
                if (s.AllUnits.TryGetValue(unitKey, out var unitDef))
                {
                    s.UnitService.Register(uid,
                        new UnitDefinition(uid, unitDef.Name,
                            unitDef.MaxHealth, unitDef.Strength, unitDef.PhysicalDefense, unitDef.Luck, unitDef.Color));
                    UnitDatabaseLoader.RegisterUnitWords(enemyResolver, unitKey);
                }
                else
                {
                    s.UnitService.Register(uid,
                        new UnitDefinition(uid, evt.EnemyName.ToUpperInvariant(),
                            s.EntityStats.GetStat(evt.EntityId, StatType.MaxHealth), 0, 0, 0, Color.red));
                }
            }));

            // Build encounter and start
            var encounterDef = BuildEncounterDefinition(s.AllUnits);
            _encounterService.StartEncounter(encounterDef, playerId);

            // Register passives for spawned enemies
            foreach (var enemyId in _encounterService.EnemyEntities)
            {
                var unitKey = enemyId.Value;
                foreach (var (key, unitDef) in s.AllUnits)
                {
                    if (unitKey.StartsWith($"enemy_{key.ToLowerInvariant()}_") && unitDef.Passives != null)
                    {
                        s.PassiveService.RegisterPassives(enemyId, unitDef.Passives);
                        break;
                    }
                }
            }

            // CombatAI
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(s.StatusEffects);
            _combatAI = new CombatAIService(s.EventBus, _encounterService, s.EntityStats,
                s.TurnService, s.SlotService, s.CombatContext, s.ActionExecution,
                scorers, enemyResolver, s.AllUnits, s.PassiveService);

            // CombatLoop
            _combatLoop = new CombatLoopService(
                s.EventBus, s.TurnService, s.EntityStats, s.WordResolver,
                s.WeaponService, playerId, s.ConsumableService);
            _combatLoop.Start();

            // --- Build UI ---
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

            _slotVisual = new CombatSlotVisual(s.UnitService, s.EntityStats, s.SlotService);
            _slotVisual.BuildEnemyRow(enemyRow);

            // Middle area
            var middleArea = new VisualElement();
            middleArea.style.flexDirection = FlexDirection.Row;
            middleArea.style.flexGrow = 1;

            _playerStatsBar = new PlayerStatsBarVisual(s.EventBus, s.EntityStats, s.StatusEffects, playerId);

            _equipment = new EquipmentVisualController(s.EventBus, s.EquipmentService,
                s.InventoryService, s.ItemRegistry, s.WeaponService, s.ConsumableService,
                s.PlayerInventoryId, playerId, root);
            _equipment.BuildBars(middleArea);

            _wordInput = new WordInputController(s.EventBus, s.WordInputService, s.DrunkLetterService,
                s.WordMatchService, s.AmmoMatchService, s.WordResolver, s.CombatContext,
                s.PreviewService, s.AmmoPreviewService, s.WeaponService, s.ConsumableService,
                s.LetterReserve, _playerStatsBar, _slotVisual, playerId, fontScaleFactor);

            var mainTextPanel = _wordInput.BuildInputArea(vibrationAmplitude);
            middleArea.Insert(1, mainTextPanel);

            _equipment.FireWeaponAction = () => _wordInput.FireWeapon();
            _equipment.UseConsumableAction = () => _wordInput.UseConsumable();
            _wordInput.SetCombatLoop(_combatLoop);

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
            root.Add(_playerStatsBar.Build());

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            Func<EntityId, Vector3> positionProvider = entityId =>
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
            s.AnimationService.Initialize(positionProvider, projectileOverlay);

            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);
            s.StatusVisualService.Initialize(positionProvider, statusTextOverlay, _slotVisual);

            _messages = new GameMessageController(s.EventBus, positionProvider, playerId);
            root.Add(_messages.CreateOverlay());

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

            var elementAnimator = new ElementAnimator();
            _frameworkTooltipService = new TooltipService(s.EventBus, elementAnimator);
            _frameworkTooltipService.SetTooltipLayer(tooltipLayer);

            _tooltipService = new EntityTooltipService(s.EventBus);
            _tooltipService.Initialize(
                _slotVisual.GetAllSlotElements(),
                _equipment.RightBar.GetAllSlotElements(),
                _equipment.LeftBar.GetAllSlotElements(),
                _frameworkTooltipService,
                s.SlotService, s.StatusEffects, s.UnitService, s.PassiveService,
                _encounterService, s.ActionRegistry, s.HandlerRegistry, s.EnemyResolver,
                s.AmmoResolver, s.WeaponService, s.ConsumableService, s.EquipmentService,
                s.ItemRegistry, s.EntityStats, s.InventoryService, s.PlayerInventoryId, playerId);

            _loot = new LootOverlayController(s.EventBus, s.LootRewardService,
                _equipment.RightBar, root, enabled => _wordInput.SetInputEnabled(enabled));

            // Combat-specific subscriptions
            _subscriptions.Add(s.EventBus.Subscribe<GameOverEvent>(_ =>
            {
                _wordInput.SetInputEnabled(false);
                _playerStatsBar.HpLabel.text = "DEAD";
                _playerStatsBar.HpLabel.style.color = Color.red;
                Debug.Log("[Turn] GAME OVER — Player died!");
            }));
            _subscriptions.Add(s.EventBus.Subscribe<EncounterStartedEvent>(evt =>
                Debug.Log($"[Encounter] Started: {evt.EncounterId} ({evt.EnemyCount} enemies)")));
            _subscriptions.Add(s.EventBus.Subscribe<EncounterEndedEvent>(evt =>
            {
                Debug.Log($"[Encounter] Ended: {evt.EncounterId} (victory={evt.Victory})");
                if (evt.Victory) ShowVictoryOverlay();
            }));
            _subscriptions.Add(s.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (_encounterService.IsEnemy(evt.EntityId))
                    Debug.Log($"[Encounter] Enemy died: {evt.EntityId.Value}");
            }));

            Debug.Log($"[CombatEncounterLiveScenario] Started — {encounterDef.DisplayName}");
        }

        private static EncounterDefinition BuildEncounterDefinition(Dictionary<string, EntityDefinition> allUnits)
        {
            var enemyKeys = new[] { "goblin", "skeleton", "bat" };
            var enemies = new List<EntityDefinition>();
            foreach (var key in enemyKeys)
            {
                if (allUnits.TryGetValue(key, out var def))
                    enemies.Add(def);
            }

            if (enemies.Count == 0)
            {
                enemies.Add(new EntityDefinition("Goblin", 30, 5, 3, 2, 1, 1, Color.green,
                    new[] { "SCRATCH", "BITE" }));
                enemies.Add(new EntityDefinition("Skeleton", 25, 4, 2, 1, 1, 1, Color.white,
                    new[] { "SCRATCH" }));
            }

            return new EncounterDefinition("scenario_combat", "Combat Encounter", enemies.ToArray());
        }

        private void ShowVictoryOverlay()
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.pickingMode = PickingMode.Ignore;

            var label = new Label("VICTORY");
            label.style.fontSize = 64;
            label.style.color = new Color(1f, 0.85f, 0.2f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.pickingMode = PickingMode.Ignore;
            overlay.Add(label);

            RootVisualElement.Add(overlay);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _wordInput?.CodeField != null,
                    _wordInput?.CodeField != null ? null : "Code field is null"),
                new("CombatLoopService created", _combatLoop != null,
                    _combatLoop != null ? null : "CombatLoopService is null"),
                new("Not game over at start", !(_combatLoop?.IsGameOver ?? true),
                    !(_combatLoop?.IsGameOver ?? true) ? null : "Should not be game over at start"),
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

            _loot?.Dispose();
            _loot = null;
            _messages?.Dispose();
            _messages = null;
            _combat?.Dispose();
            _combat = null;
            _equipment?.Dispose();
            _equipment = null;
            _wordInput?.Dispose();
            _wordInput = null;
            _playerStatsBar?.Dispose();
            _playerStatsBar = null;

            (_combatLoop as IDisposable)?.Dispose();
            (_combatAI as IDisposable)?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            _combatLoop = null;
            _combatAI = null;
            _encounterService = null;

            _session?.Dispose();
            _session = null;
            _slotVisual = null;
        }

        private sealed class LiveAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
