using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.EventEncounter.Scenarios
{
    internal sealed class EventEncounterScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private IEventEncounterService _encounterService;
        private IEntityStatsService _entityStats;
        private EntityId _playerId;
        private bool _encounterStarted;
        private int _interactableCount;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<string> _eventLog = new();

        public EventEncounterScenario() : base(new TestScenarioDefinition(
            "event-encounter",
            "Event Encounter",
            "Tests event encounter setup: interactables in slots, reactions to actions, outcomes firing.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventLog.Clear();
            _encounterStarted = false;
            _interactableCount = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            var turnService = new TurnService(_eventBus);
            var slotService = new CombatSlotService(_eventBus);
            slotService.Initialize();

            var combatContext = new CombatContext();
            combatContext.SetEntityStats(_entityStats);
            combatContext.SetSlotService(slotService);

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, turnService, _eventBus);
            var statusEffects = new StatusEffectService(
                _eventBus, _entityStats, turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(
                _entityStats, slotService, _eventBus, statusEffects);
            var reactionService = new ReactionService(_eventBus, outcomeRegistry, encounterContext, tagReactions);

            _encounterService = new EventEncounterService(
                _eventBus, _entityStats, slotService, combatContext, reactionService);
            encounterContext.EncounterService = (IEventEncounterService)_encounterService;

            _playerId = new EntityId("player");
            PlayerDefaults.Register(_entityStats, _playerId);

            _subscriptions.Add(_eventBus.Subscribe<EventEncounterStartedEvent>(evt =>
            {
                _encounterStarted = true;
                _interactableCount = evt.InteractableCount;
                var msg = $"Encounter started: {evt.EncounterId} ({evt.InteractableCount} interactables)";
                _eventLog.Add(msg);
                Debug.Log($"[EventEncounterScenario] {msg}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<InteractionMessageEvent>(evt =>
            {
                var msg = $"Message: {evt.Message}";
                _eventLog.Add(msg);
                Debug.Log($"[EventEncounterScenario] {msg}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<RewardGrantedEvent>(evt =>
            {
                var msg = $"Reward: {evt.RewardType} x{evt.Value}";
                _eventLog.Add(msg);
                Debug.Log($"[EventEncounterScenario] {msg}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<EventEncounterTransitionEvent>(evt =>
            {
                var msg = $"Transition to: {evt.TargetEncounterId}";
                _eventLog.Add(msg);
                Debug.Log($"[EventEncounterScenario] {msg}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<EventEncounterEndedEvent>(evt =>
            {
                var msg = $"Encounter ended: {evt.EncounterId}";
                _eventLog.Add(msg);
                Debug.Log($"[EventEncounterScenario] {msg}");
            }));

            var encounter = CreateTestEncounter();
            _encounterService.StartEncounter(encounter, _playerId);

            var interactables = _encounterService.InteractableEntities;

            if (interactables.Count > 2)
            {
                _eventBus.Publish(new InteractionActionEvent(
                    _playerId, "Pray", new[] { interactables[2] }, 1, "pray"));
            }

            if (interactables.Count > 1)
            {
                _eventBus.Publish(new InteractionActionEvent(
                    _playerId, "Open", new[] { interactables[1] }, 1, "open"));
            }

            BuildUI();
        }

        private static EventEncounterDefinition CreateTestEncounter()
        {
            return new EventEncounterDefinition(
                "roadside_inn",
                "Roadside Inn",
                new[]
                {
                    new InteractableDefinition(
                        "Inn", 5, new Color(0.8f, 0.6f, 0.2f),
                        new[]
                        {
                            new InteractionReaction("Enter", "transition", "inn_interior", 0),
                            new InteractionReaction("Talk", "message", "The innkeeper nods warmly.", 0),
                        },
                        "A cozy roadside inn"),
                    new InteractableDefinition(
                        "Chest", 3, new Color(0.6f, 0.4f, 0.1f),
                        new[]
                        {
                            new InteractionReaction("Open", "reward", "gold", 25),
                            new InteractionReaction("Open", "consume", null, 0),
                            new InteractionReaction("Steal", "reward", "gold", 10, 0.5f),
                        },
                        "A dusty wooden chest"),
                    new InteractableDefinition(
                        "Shrine", 10, new Color(0.4f, 0.6f, 0.9f),
                        new[]
                        {
                            new InteractionReaction("Pray", "heal", null, 20),
                            new InteractionReaction("Pray", "mana", null, 10),
                            new InteractionReaction("Pray", "message", "The shrine glows softly.", 0),
                        },
                        "An ancient stone shrine"),
                }
            );
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
            root.style.paddingRight = 20;

            var title = new Label("Event Encounter");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Active", _encounterService.IsEncounterActive.ToString(), new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "Interactables", _interactableCount.ToString(), new Color(0.8f, 0.6f, 0.2f));
            AddInfoRow(root, "Player HP",
                $"{_entityStats.GetCurrentHealth(_playerId)}/{_entityStats.GetStat(_playerId, StatType.MaxHealth)}",
                new Color(0.2f, 0.8f, 0.2f));

            var entitiesTitle = new Label("Interactable Entities:");
            entitiesTitle.style.fontSize = 16;
            entitiesTitle.style.color = Color.white;
            entitiesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            entitiesTitle.style.marginTop = 12;
            entitiesTitle.style.marginBottom = 8;
            root.Add(entitiesTitle);

            foreach (var entity in _encounterService.InteractableEntities)
            {
                var def = _encounterService.GetDefinition(entity);
                var hp = _entityStats.GetCurrentHealth(entity);
                var label = new Label($"  {def.Name} ({entity.Value}) HP={hp} — {def.Description ?? ""}");
                label.style.fontSize = 14;
                label.style.color = def.Color;
                label.style.marginBottom = 4;
                root.Add(label);
            }

            var logTitle = new Label("Event Log:");
            logTitle.style.fontSize = 16;
            logTitle.style.color = Color.white;
            logTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            logTitle.style.marginTop = 12;
            logTitle.style.marginBottom = 8;
            root.Add(logTitle);

            foreach (var entry in _eventLog)
            {
                var label = new Label($"  {entry}");
                label.style.fontSize = 14;
                label.style.color = Color.green;
                label.style.marginBottom = 4;
                root.Add(label);
            }
        }

        private static void AddInfoRow(VisualElement parent, string label, string value, Color valueColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;

            var nameLabel = new Label($"{label}: ");
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.width = 150;
            row.Add(nameLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 16;
            valueLabel.style.color = valueColor;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Encounter started", _encounterStarted,
                    _encounterStarted ? null : "Encounter did not start"),
                new("3 interactables", _interactableCount == 3,
                    _interactableCount == 3 ? null : $"Expected 3 interactables, got {_interactableCount}"),
                new("Encounter is active", _encounterService.IsEncounterActive,
                    _encounterService.IsEncounterActive ? null : "Encounter is not active"),
                new("Entities registered", _encounterService.InteractableEntities.Count == 3,
                    _encounterService.InteractableEntities.Count == 3
                        ? null : $"Expected 3 entities, got {_encounterService.InteractableEntities.Count}"),
                new("Reactions fired", _eventLog.Count > 1,
                    _eventLog.Count > 1 ? null : $"Expected reactions to fire, got {_eventLog.Count} log entries"),
                new("Heal outcome applied", _entityStats.GetCurrentHealth(_playerId) == 100,
                    _entityStats.GetCurrentHealth(_playerId) == 100
                        ? null : $"Player HP should be 100 (healed to cap), got {_entityStats.GetCurrentHealth(_playerId)}"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventLog.Clear();

            (_encounterService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            _encounterService = null;
            _entityStats = null;
            _eventBus?.ClearAllSubscriptions();
            _eventBus = null;
        }
    }
}
