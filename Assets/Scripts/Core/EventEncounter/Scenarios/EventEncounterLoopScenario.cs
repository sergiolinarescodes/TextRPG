using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Scenarios
{
    internal sealed class EventEncounterLoopScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private ICombatLoopService _combatLoop;
        private IEventEncounterService _encounterService;
        private IEntityStatsService _entityStats;
        private EntityId _playerId;

        private bool _loopStarted;
        private WordSubmitResult _validWordResult;
        private WordSubmitResult _invalidWordResult;
        private WordSubmitResult _duringAnimationResult;
        private bool _loopActiveAfterSubmit;

        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<string> _eventLog = new();
        private IDisposable _combatAIDisposable;

        public EventEncounterLoopScenario() : base(new TestScenarioDefinition(
            "event-encounter-loop",
            "Event Encounter Loop (Unified)",
            "Tests unified combat loop with event encounter entities: " +
            "start, submit valid word, reject invalid word, animation-wait flag, interactable turns pass.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventLog.Clear();
            _loopStarted = false;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            var slotService = new CombatSlotService(_eventBus);
            var combatContext = new CombatContext();
            var turnService = new TurnService(_eventBus);

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry(null);
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(_entityStats, slotService, _eventBus, null);
            var reactionService = new ReactionService(_eventBus, outcomeRegistry, encounterContext, tagReactions);
            _encounterService = new EventEncounterService(
                _eventBus, _entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = _encounterService;

            var wordData = WordActionTestFactory.CreateTestData();

            _playerId = new EntityId("player");
            PlayerDefaults.Register(_entityStats, _playerId);

            // Encounter adapter (no auto-end)
            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(_playerId);
            encounterAdapter.SetEventBus(_eventBus);
            encounterAdapter.SetAutoEndOnAllDead(false);

            // Start encounter
            var encounter = new EventEncounterDefinition("test_room", "Test Room", new[]
            {
                new InteractableDefinition("Door", 5, Color.white, Array.Empty<InteractionReaction>()),
            });
            _encounterService.StartEncounter(encounter, _playerId);

            // Register interactables as enemies in adapter
            for (int i = 0; i < encounter.Interactables.Length; i++)
            {
                var def = encounter.Interactables[i];
                var entityId = _encounterService.InteractableEntities[i];
                var entityDef = new EntityDefinition(
                    def.Name, def.MaxHealth, 0, 0, 0, 0, 0, def.Color,
                    Array.Empty<string>());
                encounterAdapter.RegisterEnemy(entityId, entityDef);
            }
            encounterAdapter.Activate();

            // CombatAI (interactables auto-pass since empty abilities)
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(null);
            var combatAI = new CombatAIService(_eventBus, encounterAdapter, _entityStats,
                turnService, slotService, combatContext, null, scorers, null);
            _combatAIDisposable = combatAI;

            // Turn order
            var turnOrder = new List<EntityId> { _playerId };
            turnOrder.AddRange(_encounterService.InteractableEntities);
            turnService.SetTurnOrder(turnOrder);

            // CombatLoopService (unified)
            var loopService = new CombatLoopService(
                _eventBus, turnService, _entityStats, wordData.Resolver, null, _playerId);
            _combatLoop = loopService;

            _subscriptions.Add(_eventBus.Subscribe<WordSubmittedEvent>(evt =>
            {
                _eventLog.Add($"Word submitted: {evt.Word}");
                Debug.Log($"[EventEncounterLoopScenario] Word submitted: {evt.Word}");
            }));

            // Start loop
            loopService.Start();
            _loopStarted = _combatLoop.IsPlayerTurn;

            // Submit invalid word
            _invalidWordResult = _combatLoop.SubmitWord("zzzznotaword");

            // Submit a valid word (from test data)
            var testWord = GetFirstTestWord(wordData.Resolver);
            if (testWord != null)
            {
                _validWordResult = _combatLoop.SubmitWord(testWord);
                _loopActiveAfterSubmit = !_combatLoop.IsGameOver;

                // While waiting for animation, another submit should be rejected
                // (not player turn — turn advanced to interactable)
                _duringAnimationResult = _combatLoop.SubmitWord(testWord);

                // Simulate animation completed — triggers turn advance through interactables back to player
                _eventBus.Publish(new ActionAnimationCompletedEvent(testWord));
            }
            else
            {
                _validWordResult = WordSubmitResult.InvalidWord;
                _loopActiveAfterSubmit = false;
                _duringAnimationResult = WordSubmitResult.InvalidWord;
            }

            BuildUI();
        }

        private static string GetFirstTestWord(IWordResolver resolver)
        {
            string[] candidates = { "ember", "bandage", "spark", "tsunami", "inferno", "abyss", "absorb" };
            foreach (var word in candidates)
            {
                if (resolver.HasWord(word))
                    return word;
            }
            return null;
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

            var title = new Label("Event Encounter Loop (Unified)");
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
                new("Loop started (player turn)", _loopStarted,
                    _loopStarted ? null : "Loop did not start — not player turn"),
                new("Invalid word rejected", _invalidWordResult == WordSubmitResult.InvalidWord,
                    _invalidWordResult == WordSubmitResult.InvalidWord
                        ? null : $"Expected InvalidWord, got {_invalidWordResult}"),
                new("Valid word accepted", _validWordResult == WordSubmitResult.Accepted,
                    _validWordResult == WordSubmitResult.Accepted
                        ? null : $"Expected Accepted, got {_validWordResult}"),
                new("Loop active after submit", _loopActiveAfterSubmit,
                    _loopActiveAfterSubmit ? null : "Loop should still be active after submit"),
                new("Blocked during animation", _duringAnimationResult == WordSubmitResult.NotPlayerTurn,
                    _duringAnimationResult == WordSubmitResult.NotPlayerTurn
                        ? null : $"Expected NotPlayerTurn during animation, got {_duringAnimationResult}"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventLog.Clear();

            (_combatLoop as IDisposable)?.Dispose();
            _combatAIDisposable?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            _combatLoop = null;
            _combatAIDisposable = null;
            _encounterService = null;
            _entityStats = null;
            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
        }
    }
}
