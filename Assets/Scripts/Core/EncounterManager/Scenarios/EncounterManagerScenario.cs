using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EncounterManager.Scenarios
{
    internal sealed class EncounterManagerScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEncounterManager _manager;
        private ICombatModeService _combatMode;
        private IEntityStatsService _entityStats;
        private EntityId _playerId;

        private bool _startedEvent;
        private bool _transitionedToCombat;
        private bool _combatModeCorrectAfterEvent;
        private bool _combatModeCorrectAfterCombat;
        private bool _returnedToEvent;

        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<string> _eventLog = new();

        public EncounterManagerScenario() : base(new TestScenarioDefinition(
            "encounter-manager",
            "Encounter Manager",
            "Tests encounter manager lifecycle: start event, transition to combat, verify mode flag, return to event.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventLog.Clear();
            _startedEvent = false;
            _transitionedToCombat = false;
            _combatModeCorrectAfterEvent = false;
            _combatModeCorrectAfterCombat = false;
            _returnedToEvent = false;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            var slotService = new CombatSlotService(_eventBus);
            var combatContext = new CombatContext();
            var turnService = new TurnService(_eventBus);
            var enemyResolver = new EnemyWordResolver();

            var combatEncounter = new EncounterService(
                _eventBus, _entityStats, turnService, slotService, combatContext, enemyResolver);

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(_entityStats, slotService, _eventBus, null);
            var reactionService = new ReactionService(_eventBus, outcomeRegistry, encounterContext, tagReactions);
            var eventEncounter = new EventEncounterService(
                _eventBus, _entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = eventEncounter;

            var providerRegistry = EventEncounterSystemInstaller.CreateProviderRegistry();
            var manager = new EncounterManager(_eventBus, combatEncounter, eventEncounter, providerRegistry);
            _manager = manager;
            _combatMode = manager;

            _playerId = new EntityId("player");
            PlayerDefaults.Register(_entityStats, _playerId);

            _subscriptions.Add(_eventBus.Subscribe<CombatModeChangedEvent>(evt =>
            {
                _eventLog.Add($"CombatMode → {evt.IsInCombat}");
                Debug.Log($"[EncounterManagerScenario] CombatMode → {evt.IsInCombat}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<EventEncounterStartedEvent>(evt =>
            {
                _eventLog.Add($"Event started: {evt.EncounterId}");
                Debug.Log($"[EncounterManagerScenario] Event started: {evt.EncounterId}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<EventEncounterEndedEvent>(evt =>
            {
                _eventLog.Add($"Event ended: {evt.EncounterId}");
                Debug.Log($"[EncounterManagerScenario] Event ended: {evt.EncounterId}");
            }));

            // Step 1: Start event encounter
            var eventEncounterDef = new EventEncounterDefinition("test_inn", "Test Inn", new[]
            {
                new InteractableDefinition("Innkeeper", 5, Color.yellow, Array.Empty<InteractionReaction>()),
            });
            _manager.StartEventEncounter(eventEncounterDef, _playerId);
            _startedEvent = _manager.IsInEvent;
            _combatModeCorrectAfterEvent = !_combatMode.IsInCombat;

            // Step 2: Transition to combat
            var combatDef = new EncounterDefinition("test_fight", "Test Fight", Array.Empty<EntityDefinition>());
            _manager.TransitionToCombat(combatDef);
            _transitionedToCombat = !_manager.IsInEvent;
            _combatModeCorrectAfterCombat = _combatMode.IsInCombat;

            // Step 3: Simulate combat end — publish the event
            _eventBus.Publish(new EncounterEndedEvent("test_fight", true));
            _returnedToEvent = _manager.IsInEvent;

            BuildUI();
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;

            var title = new Label("Encounter Manager");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            foreach (var entry in _eventLog)
            {
                var label = new Label($"  {entry}");
                label.style.fontSize = 14;
                label.style.color = Color.green;
                label.style.marginBottom = 4;
                root.Add(label);
            }
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Event encounter started", _startedEvent,
                    _startedEvent ? null : "Event encounter did not start"),
                new("Combat mode false after event start", _combatModeCorrectAfterEvent,
                    _combatModeCorrectAfterEvent ? null : "IsInCombat should be false after event start"),
                new("Transitioned to combat", _transitionedToCombat,
                    _transitionedToCombat ? null : "Event encounter should have ended on combat transition"),
                new("Combat mode true after combat start", _combatModeCorrectAfterCombat,
                    _combatModeCorrectAfterCombat ? null : "IsInCombat should be true after combat transition"),
                new("Returned to event after combat end", _returnedToEvent,
                    _returnedToEvent ? null : "Should return to event encounter after combat ends"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventLog.Clear();

            (_manager as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            _manager = null;
            _combatMode = null;
            _entityStats = null;
            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
        }
    }
}
