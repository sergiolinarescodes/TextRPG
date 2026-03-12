using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Scenarios
{
    internal sealed class ActionExecutionScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter WordParam = new(
            "word", "Word", typeof(string), "tsunami");
        private static readonly ScenarioParameter SourceStrengthParam = new(
            "sourceStrength", "Source Strength", typeof(int), 10, 1, 50);

        private IActionExecutionService _executionService;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private IStatusEffectService _statusEffects;
        private IEventBus _eventBus;
        private EntityId _hero;
        private EntityId _enemy;
        private string _word;
        private int _sourceStrength;
        private int _initialEnemyHealth;
        private bool _wordResolved;
        private bool _executionStarted;
        private bool _executionCompleted;
        private int _handlersExecuted;
        private readonly List<IDisposable> _subscriptions = new();

        public ActionExecutionScenario() : base(new TestScenarioDefinition(
            "action-execution-pipeline",
            "Action Execution Pipeline",
            "Submits a word, resolves actions, executes handlers, and displays before/after entity state.",
            new[] { WordParam, SourceStrengthParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _word = ResolveParam<string>(overrides, "word");
            _sourceStrength = ResolveParam<int>(overrides, "sourceStrength");
            _wordResolved = false;
            _executionStarted = false;
            _executionCompleted = false;
            _handlersExecuted = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            _statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)_statusEffects;

            var wordData = WordActionTestFactory.CreateTestData();
            var actionHandlerRegistry = ActionExecutionTestFactory.CreateHandlerRegistry(_eventBus, _entityStats, _statusEffects);
            var combatContext = new CombatContext();

            _hero = new EntityId("hero");
            _enemy = new EntityId("enemy");

            _entityStats.RegisterEntity(_hero, maxHealth: 100, strength: _sourceStrength, magicPower: 8,
                physicalDefense: 5, magicDefense: 4, luck: 3);
            _entityStats.RegisterEntity(_enemy, maxHealth: 100, strength: 8, magicPower: 6,
                physicalDefense: 4, magicDefense: 3, luck: 2);

            _turnService.SetTurnOrder(new[] { _hero, _enemy });
            _initialEnemyHealth = _entityStats.GetCurrentHealth(_enemy);

            combatContext.SetSourceEntity(_hero);
            combatContext.SetEnemies(new[] { _enemy });
            combatContext.SetAllies(Array.Empty<EntityId>());

            _executionService = new ActionExecutionService(_eventBus, wordData.Resolver, actionHandlerRegistry, combatContext);

            _subscriptions.Add(_eventBus.Subscribe<WordResolvedEvent>(e =>
            {
                _wordResolved = true;
                Debug.Log($"[ActionExecutionScenario] Word resolved: {e.Word} → {e.Actions.Count} actions (target: {e.Stats.Target})");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ActionExecutionStartedEvent>(e =>
            {
                _executionStarted = true;
                Debug.Log($"[ActionExecutionScenario] Execution started: {e.Word} ({e.ActionCount} actions)");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ActionHandlerExecutedEvent>(e =>
            {
                _handlersExecuted++;
                Debug.Log($"[ActionExecutionScenario] Handler executed: {e.ActionId} (value={e.Value}) on {e.Targets.Count} targets");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                _executionCompleted = true;
                Debug.Log($"[ActionExecutionScenario] Execution completed: {e.Word}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(e =>
                Debug.Log($"[ActionExecutionScenario] {e.EntityId.Value} took {e.Amount} damage, HP={e.RemainingHealth}")));
            _subscriptions.Add(_eventBus.Subscribe<StatusEffectAppliedEvent>(e =>
                Debug.Log($"[ActionExecutionScenario] {e.Type} applied to {e.Target.Value}")));
            _subscriptions.Add(_eventBus.Subscribe<PushActionEvent>(e =>
                Debug.Log($"[ActionExecutionScenario] Push: {e.Source.Value} → {e.Target.Value} (force={e.Value})")));

            _executionService.ExecuteWord(_word);

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

            var title = new Label("Action Execution Pipeline");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Word", _word, new Color(0.6f, 0.8f, 1f));
            AddInfoRow(root, "Handlers Run", _handlersExecuted.ToString(), new Color(1f, 0.85f, 0.3f));
            AddInfoRow(root, "Hero HP", $"{_entityStats.GetCurrentHealth(_hero)}/100", new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "Enemy HP",
                $"{_entityStats.GetCurrentHealth(_enemy)}/{_initialEnemyHealth}",
                new Color(1f, 0.3f, 0.3f));

            var effectsTitle = new Label("Enemy Status Effects:");
            effectsTitle.style.fontSize = 16;
            effectsTitle.style.color = Color.white;
            effectsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            effectsTitle.style.marginTop = 12;
            effectsTitle.style.marginBottom = 8;
            root.Add(effectsTitle);

            var effects = _statusEffects.GetEffects(_enemy);
            if (effects.Count == 0)
            {
                var noEffects = new Label("  (none)");
                noEffects.style.fontSize = 14;
                noEffects.style.color = new Color(0.5f, 0.5f, 0.5f);
                root.Add(noEffects);
            }
            else
            {
                foreach (var effect in effects)
                {
                    var durationText = effect.IsPermanent ? "permanent" : $"{effect.RemainingDuration} turns";
                    var label = new Label($"  {effect.Type} — {durationText}");
                    label.style.fontSize = 14;
                    label.style.color = new Color(0.8f, 0.6f, 0.2f);
                    label.style.marginBottom = 4;
                    root.Add(label);
                }
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
                new("Execution service initialized", _executionService != null,
                    _executionService != null ? null : "Service is null"),
                new("Word was resolved", _wordResolved,
                    _wordResolved ? null : "Word was not resolved"),
                new("Execution started", _executionStarted,
                    _executionStarted ? null : "Execution did not start"),
                new("Execution completed", _executionCompleted,
                    _executionCompleted ? null : "Execution did not complete"),
                new("Handlers were executed", _handlersExecuted > 0,
                    _handlersExecuted > 0 ? null : "No handlers executed"),
                new("Enemy state changed", _entityStats.GetCurrentHealth(_enemy) != _initialEnemyHealth
                    || _statusEffects.GetEffects(_enemy).Count > 0,
                    "Enemy health or effects should have changed"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_executionService as IDisposable)?.Dispose();
            (_statusEffects as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _executionService = null;
            _statusEffects = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
