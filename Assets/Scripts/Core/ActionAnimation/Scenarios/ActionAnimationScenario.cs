using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionAnimation.Scenarios
{
    internal sealed class ActionAnimationScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter AnimDurationParam = new(
            "animDuration", "Animation Duration", typeof(float), 0.4f, 0.1f, 2f);

        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private ICombatSlotService _slotService;
        private IActionExecutionService _actionExecution;
        private ActionAnimationService _animationService;
        private CombatSlotVisual _slotVisual;
        private IUnitService _unitService;
        private readonly List<IDisposable> _subscriptions = new();

        private EntityId _player;
        private readonly List<EntityId> _enemies = new();
        private bool _animationStarted;
        private bool _animationCompleted;
        private string _completedWord;

        public ActionAnimationScenario() : base(new TestScenarioDefinition(
            "action-animation",
            "Action Animation",
            "Spawns player + 3 enemies in slots, executes a word, and shows projectile " +
            "animations flying from source to targets.",
            new[] { AnimDurationParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var animDuration = ResolveParam<float>(overrides, "animDuration");

            _animationStarted = false;
            _animationCompleted = false;
            _completedWord = null;
            _enemies.Clear();

            // --- Services ---
            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            var turnService = new TurnService(_eventBus);
            _unitService = new UnitService(_eventBus);
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            var combatContext = new CombatContext();
            combatContext.SetEntityStats(_entityStats);
            combatContext.SetSlotService(_slotService);

            // StatusEffect
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, turnService, _eventBus);
            var statusEffects = new StatusEffectService(_eventBus, _entityStats, turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            // Word data + action handler registry
            var wordData = WordActionTestFactory.CreateTestData();
            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, combatContext, statusEffects, turnService);
            var handlerRegistry = ActionHandlerRegistryFactory.CreateDefault(actionHandlerCtx);

            // --- Entities ---
            _player = new EntityId("player");
            _entityStats.RegisterEntity(_player, maxHealth: 100, strength: 10, magicPower: 8,
                physicalDefense: 5, magicDefense: 4, luck: 3);

            var enemyNames = new[] { "enemy_a", "enemy_b", "enemy_c" };
            for (int i = 0; i < enemyNames.Length; i++)
            {
                var entityId = new EntityId(enemyNames[i]);
                _entityStats.RegisterEntity(entityId, maxHealth: 50, strength: 6, magicPower: 4,
                    physicalDefense: 3, magicDefense: 2, luck: 2);
                _unitService.Register(new UnitId(enemyNames[i]),
                    new UnitDefinition(new UnitId(enemyNames[i]), enemyNames[i].ToUpperInvariant(), 50, 6, 4, 2, Color.red));
                _slotService.RegisterEnemy(entityId, i);
                _enemies.Add(entityId);
            }

            combatContext.SetSourceEntity(_player);
            combatContext.SetEnemies(_enemies.ToArray());
            combatContext.SetAllies(Array.Empty<EntityId>());

            turnService.SetTurnOrder(new[] { _player });

            // --- Action Execution (deferred mode) ---
            var runtimeResolver = new RuntimeAnimationResolver();
            _actionExecution = new ActionExecutionService(_eventBus, wordData.Resolver, handlerRegistry, combatContext, _entityStats, statusEffects, runtimeResolver);

            // --- Action Animation ---
            _animationService = new ActionAnimationService(_eventBus, runtimeResolver, handlerRegistry);

            var tickRunner = SceneRoot.AddComponent<TickRunner>();
            tickRunner.Initialize(new UnityTimeProvider(), new ITickable[] { _animationService });

            // --- Events ---
            _subscriptions.Add(_eventBus.Subscribe<ActionAnimationStartedEvent>(e =>
            {
                _animationStarted = true;
                Debug.Log($"[ActionAnimationScenario] Animation started: {e.Word} ({e.ActionCount} actions)");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ActionAnimationCompletedEvent>(e =>
            {
                _animationCompleted = true;
                _completedWord = e.Word;
                Debug.Log($"[ActionAnimationScenario] Animation completed: {e.Word}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<ActionHandlerExecutedEvent>(e =>
            {
                Debug.Log($"[ActionAnimationScenario] Handler: {e.ActionId}({e.Value}) → {e.Targets.Count} target(s)");
            }));
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(e =>
            {
                Debug.Log($"[ActionAnimationScenario] {e.EntityId.Value} took {e.Amount} damage, HP={e.RemainingHealth}");
            }));

            // --- Build UI ---
            BuildUI();

            // --- Execute test word ---
            _actionExecution.ExecuteWord("inferno");
        }

        private void BuildUI()
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f);

            // Info panel
            var infoPanel = new VisualElement();
            infoPanel.style.paddingTop = 20;
            infoPanel.style.paddingLeft = 20;
            root.Add(infoPanel);

            var title = new Label("Action Animation");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            infoPanel.Add(title);

            var wordLabel = new Label("Word: \"inferno\" (Damage + Burn on AllEnemies)");
            wordLabel.style.fontSize = 16;
            wordLabel.style.color = new Color(0.6f, 0.8f, 1f);
            wordLabel.style.marginBottom = 8;
            infoPanel.Add(wordLabel);

            var statusLabel = new Label(_animationStarted ? "Animation started" : "Waiting...");
            statusLabel.style.fontSize = 14;
            statusLabel.style.color = _animationCompleted ? Color.green : Color.yellow;
            statusLabel.style.marginBottom = 16;
            infoPanel.Add(statusLabel);

            // Enemy slot row
            var enemyRow = new VisualElement();
            enemyRow.style.flexDirection = FlexDirection.Row;
            enemyRow.style.justifyContent = Justify.Center;
            enemyRow.style.height = 120;
            root.Add(enemyRow);

            _slotVisual = new CombatSlotVisual(_unitService, _entityStats, _slotService);
            _slotVisual.BuildEnemyRow(enemyRow);

            // Projectile overlay (last child = renders on top of everything)
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            // Position provider: map entity → slot cell panel coords
            Func<EntityId, Vector3> positionProvider = entityId =>
            {
                var element = _slotVisual.GetSlotElement(entityId);
                if (element != null)
                {
                    var center = element.worldBound.center;
                    return new Vector3(center.x, center.y, 0f);
                }
                return Vector3.zero;
            };

            _animationService.Initialize(positionProvider, projectileOverlay);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            bool isAnimating = _animationService?.IsAnimating ?? false;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Animation service initialized", _animationService != null,
                    _animationService != null ? null : "Animation service is null"),
                new("Enemies spawned", _enemies.Count >= 1,
                    _enemies.Count >= 1 ? null : "No enemies spawned"),
                new("Animation started event received", _animationStarted,
                    _animationStarted ? null : "Animation started event not received"),
                new("Commands enqueued (animating)", isAnimating,
                    isAnimating ? null : "Command queue is empty — no animation enqueued"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            _animationService?.Dispose();
            (_actionExecution as IDisposable)?.Dispose();
            (_slotService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            _eventBus?.ClearAllSubscriptions();

            _animationService = null;
            _actionExecution = null;
            _slotService = null;
            _entityStats = null;
            _eventBus = null;
            _slotVisual = null;
        }

        private sealed class RuntimeAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
