using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.TurnSystem.Scenarios
{
    internal sealed class TurnFlowScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter EntityCountParam = new(
            "entityCount", "Entity Count", typeof(int), 3, 2, 8);

        private ITurnService _service;
        private IEventBus _eventBus;
        private List<EntityId> _entities;
        private int _entityCount;
        private readonly List<IDisposable> _subscriptions = new();
        private int _roundsCompleted;
        private bool _extraTurnGranted;

        public TurnFlowScenario() : base(new TestScenarioDefinition(
            "turn-flow",
            "Turn Flow",
            "Sets up a turn order, cycles through turns, grants an extra turn, and displays turn/round state.",
            new[] { EntityCountParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _entityCount = ResolveParam<int>(overrides, "entityCount");
            _roundsCompleted = 0;
            _extraTurnGranted = false;

            _eventBus = new EventBus();
            _service = new TurnService(_eventBus);

            _entities = new List<EntityId>();
            for (int i = 0; i < _entityCount; i++)
                _entities.Add(new EntityId($"entity-{i}"));

            _subscriptions.Add(_eventBus.Subscribe<TurnStartedEvent>(e =>
                Debug.Log($"[TurnFlowScenario] Turn {e.TurnNumber} started: {e.EntityId.Value}")));
            _subscriptions.Add(_eventBus.Subscribe<TurnEndedEvent>(e =>
                Debug.Log($"[TurnFlowScenario] Turn {e.TurnNumber} ended: {e.EntityId.Value}")));
            _subscriptions.Add(_eventBus.Subscribe<RoundStartedEvent>(e =>
            {
                _roundsCompleted++;
                Debug.Log($"[TurnFlowScenario] Round {e.RoundNumber} started");
            }));
            _subscriptions.Add(_eventBus.Subscribe<RoundEndedEvent>(e =>
                Debug.Log($"[TurnFlowScenario] Round {e.RoundNumber} ended")));
            _subscriptions.Add(_eventBus.Subscribe<ExtraTurnGrantedEvent>(e =>
            {
                _extraTurnGranted = true;
                Debug.Log($"[TurnFlowScenario] Extra turn granted to {e.EntityId.Value}");
            }));

            _service.SetTurnOrder(_entities);

            // Cycle through one full round
            for (int i = 0; i < _entityCount; i++)
            {
                _service.BeginTurn();
                _service.EndTurn();
            }

            // Grant extra turn to first entity, then do one more turn
            _service.GrantExtraTurn(_entities[0]);
            _service.BeginTurn();
            _service.EndTurn();

            // Do one more normal turn
            _service.BeginTurn();

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
            root.style.paddingRight = 20;

            var title = new Label("Turn Flow");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            var statePanel = new VisualElement();
            statePanel.style.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            statePanel.style.borderTopLeftRadius = 8;
            statePanel.style.borderTopRightRadius = 8;
            statePanel.style.borderBottomLeftRadius = 8;
            statePanel.style.borderBottomRightRadius = 8;
            statePanel.style.paddingTop = 12;
            statePanel.style.paddingBottom = 12;
            statePanel.style.paddingLeft = 16;
            statePanel.style.paddingRight = 16;
            statePanel.style.marginBottom = 12;
            root.Add(statePanel);

            AddInfoRow(statePanel, "Current Entity", _service.CurrentEntity.Value, new Color(0.6f, 0.8f, 1f));
            AddInfoRow(statePanel, "Turn Number", _service.CurrentTurnNumber.ToString(), new Color(1f, 0.85f, 0.3f));
            AddInfoRow(statePanel, "Round Number", _service.CurrentRoundNumber.ToString(), new Color(0.4f, 0.9f, 0.4f));
            AddInfoRow(statePanel, "Turn Active", _service.IsTurnActive.ToString(), new Color(0.8f, 0.8f, 0.8f));

            var orderTitle = new Label("Turn Order:");
            orderTitle.style.fontSize = 16;
            orderTitle.style.color = Color.white;
            orderTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            orderTitle.style.marginBottom = 8;
            root.Add(orderTitle);

            for (int i = 0; i < _entities.Count; i++)
            {
                var isActive = _entities[i].Value == _service.CurrentEntity.Value;
                var entityLabel = new Label($"  {(isActive ? "> " : "  ")}{_entities[i].Value}");
                entityLabel.style.fontSize = 14;
                entityLabel.style.color = isActive ? new Color(0.6f, 1f, 0.6f) : new Color(0.6f, 0.6f, 0.6f);
                entityLabel.style.marginBottom = 4;
                root.Add(entityLabel);
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
            // After: 1 full round (_entityCount turns) + 1 extra turn + 1 normal turn (still active)
            // Total turns = _entityCount + 2
            // Rounds completed: 1 (the round-end from the full cycle triggered RoundStarted for round 2)
            // The extra turn doesn't advance the index, so after it the next normal turn is entity-0 (index 0 in round 2)
            // Currently active turn on entity-0, round 2

            var expectedTurnNumber = _entityCount + 2;
            var expectedCurrentEntity = _entities[0].Value;

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Service initialized", _service != null,
                    _service != null ? null : "Service is null"),
                new("Turn is active", _service.IsTurnActive,
                    _service.IsTurnActive ? null : "Expected turn to be active"),
                new("Turn number correct", _service.CurrentTurnNumber == expectedTurnNumber,
                    _service.CurrentTurnNumber == expectedTurnNumber
                        ? null : $"Expected turn {expectedTurnNumber} but got {_service.CurrentTurnNumber}"),
                new("Round advanced", _service.CurrentRoundNumber == 2,
                    _service.CurrentRoundNumber == 2
                        ? null : $"Expected round 2 but got {_service.CurrentRoundNumber}"),
                new("Current entity correct", _service.CurrentEntity.Value == expectedCurrentEntity,
                    _service.CurrentEntity.Value == expectedCurrentEntity
                        ? null : $"Expected '{expectedCurrentEntity}' but got '{_service.CurrentEntity.Value}'"),
                new("Extra turn was granted", _extraTurnGranted,
                    _extraTurnGranted ? null : "Extra turn event was not fired"),
                new("Round end was triggered", _roundsCompleted > 0,
                    _roundsCompleted > 0 ? null : "No round transitions occurred"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_service as IDisposable)?.Dispose();
            _service = null;
            _eventBus = null;
            _entities = null;
        }
    }
}
