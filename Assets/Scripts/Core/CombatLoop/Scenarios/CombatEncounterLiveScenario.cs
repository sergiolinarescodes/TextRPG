using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Testing;
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

        private LiveScenarioServices _svc;
        private LiveScenarioLayout _layout;
        private EncounterService _encounterService;
        private ICombatLoopService _combatLoop;
        private ICombatAIService _combatAI;
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

            // Core services
            _svc = LiveScenarioHelper.CreateCoreServices(playerId, SceneRoot);

            // Load unit definitions from DB
            var allUnits = UnitDatabaseLoader.LoadAll();

            // Real EncounterService
            var enemyResolver = new EnemyWordResolver();
            _encounterService = new EncounterService(
                _svc.EventBus, _svc.EntityStats, _svc.TurnService, _svc.SlotService,
                _svc.CombatContext, enemyResolver);
            _svc.EncounterAdapter = _encounterService;
            _svc.EnemyResolver = enemyResolver;

            // Subscribe to EnemySpawnedEvent — register UnitDefinitions + enemy words
            _subscriptions.Add(_svc.EventBus.Subscribe<EnemySpawnedEvent>(evt =>
            {
                var unitKey = evt.EnemyName.ToLowerInvariant();
                var uid = new UnitId(evt.EntityId.Value);
                if (allUnits.TryGetValue(unitKey, out var unitDef))
                {
                    _svc.UnitService.Register(uid,
                        new UnitDefinition(uid, unitDef.Name,
                            unitDef.MaxHealth, unitDef.Strength, unitDef.PhysicalDefense, unitDef.Luck, unitDef.Color));
                    UnitDatabaseLoader.RegisterUnitWords(enemyResolver, unitKey);
                }
                else
                {
                    _svc.UnitService.Register(uid,
                        new UnitDefinition(uid, evt.EnemyName.ToUpperInvariant(),
                            _svc.EntityStats.GetStat(evt.EntityId, StatType.MaxHealth), 0, 0, 0, Color.red));
                }
            }));

            // Build encounter definition
            var encounterDef = BuildEncounterDefinition(allUnits);

            // Start encounter — handles entity registration, slot assignment, turn order
            _encounterService.StartEncounter(encounterDef, playerId);

            // Composite word resolver (player words + enemy words)
            var compositeResolver = new CompositeWordResolver(_svc.WordResolver, enemyResolver);

            // Action execution
            LiveScenarioHelper.CreateActionExecution(_svc, compositeResolver);

            // Passive system
            LiveScenarioHelper.CreatePassiveService(_svc, _encounterService, allUnits);

            // Register passives for spawned enemies
            foreach (var enemyId in _encounterService.EnemyEntities)
            {
                var unitKey = enemyId.Value;
                // Try to match by stripping the "enemy_" prefix and index suffix
                foreach (var (key, unitDef) in allUnits)
                {
                    if (unitKey.StartsWith($"enemy_{key.ToLowerInvariant()}_") && unitDef.Passives != null)
                    {
                        _svc.PassiveService.RegisterPassives(enemyId, unitDef.Passives);
                        break;
                    }
                }
            }

            // Equipment & loot
            LiveScenarioHelper.CreateEquipmentAndLoot(_svc);

            // Scorer registry for AI
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(_svc.StatusEffects);

            // CombatAI service
            _combatAI = new CombatAIService(_svc.EventBus, _encounterService, _svc.EntityStats,
                _svc.TurnService, _svc.SlotService, _svc.CombatContext, _svc.ActionExecution,
                scorers, enemyResolver, allUnits, _svc.PassiveService);

            // CombatLoop
            _combatLoop = new CombatLoopService(
                _svc.EventBus, _svc.TurnService, _svc.EntityStats, _svc.WordResolver,
                _svc.WeaponService, playerId, _svc.ConsumableService);
            _combatLoop.Start();

            // Build UI
            _layout = LiveScenarioHelper.BuildLayout(RootVisualElement, _svc, vibrationAmplitude, fontScaleFactor);

            // Common event subscriptions
            LiveScenarioHelper.SubscribeCommonEvents(_svc, _layout, _subscriptions,
                () => _combatLoop.IsPlayerTurn, allUnits);

            // Combat-specific subscriptions
            _subscriptions.Add(_svc.EventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
            {
                LiveScenarioHelper.SetInputEnabled(_layout, true);
                Debug.Log($"[Turn] === Player's turn (Turn #{evt.TurnNumber}, Round #{evt.RoundNumber}) ===");
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<PlayerTurnEndedEvent>(_ =>
            {
                LiveScenarioHelper.SetInputEnabled(_layout, false);
                Debug.Log("[Turn] Player's turn ended");
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<GameOverEvent>(_ =>
            {
                LiveScenarioHelper.SetInputEnabled(_layout, false);
                _layout.HpLabel.text = "DEAD";
                _layout.HpLabel.style.color = Color.red;
                Debug.Log("[Turn] GAME OVER — Player died!");
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<EncounterStartedEvent>(evt =>
                Debug.Log($"[Encounter] Started: {evt.EncounterId} ({evt.EnemyCount} enemies)")));
            _subscriptions.Add(_svc.EventBus.Subscribe<EncounterEndedEvent>(evt =>
            {
                Debug.Log($"[Encounter] Ended: {evt.EncounterId} (victory={evt.Victory})");
                if (evt.Victory)
                    ShowVictoryOverlay();
            }));

            // Handle EntityDied for encounter service tracking
            _subscriptions.Add(_svc.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                if (_encounterService.IsEnemy(evt.EntityId))
                    Debug.Log($"[Encounter] Enemy died: {evt.EntityId.Value}");
            }));

            // Input handling
            LiveScenarioHelper.SetupInputHandling(_layout, _svc,
                submitFunc: word => _combatLoop.SubmitWord(word),
                canFireWeapon: () =>
                {
                    if (!_combatLoop.FireWeapon()) return false;
                    return true;
                },
                canUseConsumable: () =>
                {
                    if (!_combatLoop.UseConsumable()) return false;
                    return true;
                },
                isEncounterActive: () => _encounterService.IsEncounterActive,
                fontScaleFactor: fontScaleFactor,
                subs: _subscriptions);

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
                // Fallback if no units in DB
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
                new("AnimatedCodeField exists", _layout?.CodeField != null,
                    _layout?.CodeField != null ? null : "Code field is null"),
                new("CombatLoopService created", _combatLoop != null,
                    _combatLoop != null ? null : "CombatLoopService is null"),
                new("Not game over at start", !(_combatLoop?.IsGameOver ?? true),
                    !(_combatLoop?.IsGameOver ?? true) ? null : "Should not be game over at start"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            (_combatLoop as IDisposable)?.Dispose();
            (_combatAI as IDisposable)?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            LiveScenarioHelper.CleanupServices(_svc, _layout, _subscriptions);

            _combatLoop = null;
            _combatAI = null;
            _encounterService = null;
            _svc = null;
            _layout = null;
        }
    }
}
