using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounterLoop.Scenarios
{
    internal sealed class EventEncounterLiveScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter VibrationAmplitudeParam = new(
            "vibrationAmplitude", "Vibration Amplitude", typeof(float), 3.0f, 0f, 10f);

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private static readonly ScenarioParameter EncounterIndexParam = new(
            "encounterIndex", "Encounter Index", typeof(int), 0, 0, 4);

        private LiveScenarioServices _svc;
        private LiveScenarioLayout _layout;
        private IEventEncounterService _encounterService;
        private IEventEncounterLoopService _loopService;
        private readonly List<IDisposable> _subscriptions = new();

        public EventEncounterLiveScenario() : base(new TestScenarioDefinition(
            "event-encounter-live",
            "Event Encounter (Live)",
            "Live interactive event encounter — type interaction words (pray, open, talk, search), " +
            "see reactions fire, interactable renders in a slot. Free-form input, no turns.",
            new[] { VibrationAmplitudeParam, FontScaleFactorParam, EncounterIndexParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
            var fontScaleFactor = ResolveParam<float>(overrides, "fontScaleFactor");
            var encounterIndex = ResolveParam<int>(overrides, "encounterIndex");

            var playerId = new EntityId("player");

            // Core services
            _svc = LiveScenarioHelper.CreateCoreServices(playerId, SceneRoot);

            // Event encounter services
            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(
                _svc.EntityStats, _svc.SlotService, _svc.EventBus, _svc.StatusEffects);
            var reactionService = new ReactionService(_svc.EventBus, outcomeRegistry, encounterContext, tagReactions);
            _encounterService = new EventEncounterService(
                _svc.EventBus, _svc.EntityStats, _svc.SlotService, _svc.CombatContext, reactionService);
            encounterContext.EncounterService = _encounterService;
            _svc.EventEncounterService = _encounterService;

            // Use an encounter adapter for passives (event encounters don't have a real IEncounterService)
            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(playerId);
            encounterAdapter.SetEventBus(_svc.EventBus);
            encounterAdapter.Activate();
            _svc.EncounterAdapter = encounterAdapter;
            _svc.EnemyResolver = new EnemyWordResolver();

            // Action execution (composite resolver = player words only, no enemy words)
            LiveScenarioHelper.CreateActionExecution(_svc, _svc.WordResolver);

            // Load unit definitions for summon registration
            var allUnits = UnitDatabaseLoader.LoadAll();

            // Passive system
            LiveScenarioHelper.CreatePassiveService(_svc, encounterAdapter, allUnits);

            // Equipment & loot
            LiveScenarioHelper.CreateEquipmentAndLoot(_svc);

            // Load encounter from provider registry
            var providerRegistry = EventEncounterSystemInstaller.CreateProviderRegistry();
            var encounterDef = LoadEncounterDefinition(providerRegistry, encounterIndex);

            // Register interactable UnitDefinitions with IUnitService for CombatSlotVisual rendering
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");
                var uid = new UnitId(entityId.Value);
                _svc.UnitService.Register(uid,
                    new UnitDefinition(uid, def.Name, def.MaxHealth, 0, 0, 0, def.Color));
            }

            // Start encounter
            _encounterService.StartEncounter(encounterDef, playerId);

            // Register interactable passives
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var entityId = _encounterService.InteractableEntities[i];
                    _svc.PassiveService.RegisterPassives(entityId, def.Passives);
                }
            }

            // Event encounter loop (free-form, no turns)
            var loopService = new EventEncounterLoopService(
                _svc.EventBus, _svc.EntityStats, _svc.WordResolver, _encounterService, playerId);
            _loopService = loopService;
            _loopService.Start();

            // Build UI
            _layout = LiveScenarioHelper.BuildLayout(RootVisualElement, _svc, vibrationAmplitude, fontScaleFactor);

            // Common event subscriptions (isPlayerTurn always true — no turns in event encounters)
            LiveScenarioHelper.SubscribeCommonEvents(_svc, _layout, _subscriptions, () => true, allUnits);

            // Event-encounter-specific subscriptions
            _subscriptions.Add(_svc.EventBus.Subscribe<InteractionMessageEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Message: {evt.Message}");
                var pos = _svc.PositionProvider?.Invoke(evt.SourceEntityId) ?? Vector3.zero;
                _svc.GameMessages?.Spawn(new Vector2(pos.x, pos.y), evt.Message, new Color(1f, 0.9f, 0.5f));
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<RewardGrantedEvent>(evt =>
                Debug.Log($"[EventEncounter] Reward: {evt.RewardType} x{evt.Value}")));
            _subscriptions.Add(_svc.EventBus.Subscribe<EventEncounterTransitionEvent>(evt =>
                Debug.Log($"[EventEncounter] Transition → {evt.TargetEncounterId}")));
            _subscriptions.Add(_svc.EventBus.Subscribe<EventEncounterEndedEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Encounter ended: {evt.EncounterId}");
                LiveScenarioHelper.SetInputEnabled(_layout, false);
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<EventEncounterStartedEvent>(evt =>
                Debug.Log($"[EventEncounter] Started: {evt.EncounterId} ({evt.InteractableCount} interactables)")));

            // Input handling — submit goes to event loop
            LiveScenarioHelper.SetupInputHandling(_layout, _svc,
                submitFunc: word => _loopService.SubmitWord(word),
                canFireWeapon: () => false,
                canUseConsumable: () => false,
                isEncounterActive: () => _encounterService.IsEncounterActive,
                fontScaleFactor: fontScaleFactor,
                subs: _subscriptions);

            Debug.Log($"[EventEncounterLiveScenario] Started — {encounterDef.DisplayName}");
        }

        private static EventEncounterDefinition LoadEncounterDefinition(
            EventEncounterProviderRegistry registry, int index)
        {
            var keys = registry.Keys.ToList();
            var key = keys[Mathf.Clamp(index, 0, keys.Count - 1)];
            return registry.Get(key).CreateDefinition();
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _layout?.CodeField != null,
                    _layout?.CodeField != null ? null : "Code field is null"),
                new("Event encounter active", _encounterService?.IsEncounterActive == true,
                    _encounterService?.IsEncounterActive == true ? null : "Encounter not active"),
                new("Loop service active", _loopService?.IsActive == true,
                    _loopService?.IsActive == true ? null : "Loop not active"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            (_loopService as IDisposable)?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            LiveScenarioHelper.CleanupServices(_svc, _layout, _subscriptions);

            _loopService = null;
            _encounterService = null;
            _svc = null;
            _layout = null;
        }
    }
}
