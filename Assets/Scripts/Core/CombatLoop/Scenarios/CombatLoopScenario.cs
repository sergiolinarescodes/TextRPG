using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
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

namespace TextRPG.Core.CombatLoop.Scenarios
{
    internal sealed class CombatLoopScenario : DataDrivenScenario
    {
        private IEventBus _eventBus;
        private ICombatLoopService _combatLoop;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private EntityId _playerId;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly List<string> _turnLog = new();

        public CombatLoopScenario() : base(new TestScenarioDefinition(
            "combat-loop",
            "Combat Loop",
            "Tests the combat turn loop: submit words, verify turn cycling, game-over on player death.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _turnLog.Clear();

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);
            var slotService = new CombatSlotService(_eventBus);
            slotService.Initialize();

            var combatContext = new CombatContext();
            combatContext.SetEntityStats(_entityStats);
            combatContext.SetSlotService(slotService);

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            var statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            var wordData = WordActionTestFactory.CreateTestData();
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordData);
            var weaponService = new WeaponService(_eventBus, weaponRegistry);

            _playerId = new EntityId("player");
            PlayerDefaults.Register(_entityStats, _playerId);

            var enemy1 = new EntityId("goblin");
            var enemy2 = new EntityId("skeleton");
            _entityStats.RegisterEntity(enemy1, 30, 5, 3, 2, 1, 1);
            _entityStats.RegisterEntity(enemy2, 25, 4, 2, 1, 1, 1);
            slotService.RegisterEnemy(enemy1, 0);
            slotService.RegisterEnemy(enemy2, 1);

            combatContext.SetSourceEntity(_playerId);
            combatContext.SetEnemies(new[] { enemy1, enemy2 });
            combatContext.SetAllies(Array.Empty<EntityId>());

            var actionHandlerCtx = new ActionHandlerContext(_entityStats, _eventBus, combatContext,
                statusEffects, _turnService, weaponService, slotService: slotService);
            var handlerRegistry = ActionHandlerFactory.CreateDefault(actionHandlerCtx);

            var animResolver = new InstantAnimationResolver();
            var actionExecution = new ActionExecutionService(
                _eventBus, wordData.Resolver, handlerRegistry, combatContext, _entityStats, statusEffects, animResolver);
            var animationService = new ActionAnimationService(_eventBus, animResolver, handlerRegistry);

            var turnOrder = new List<EntityId> { _playerId, enemy1, enemy2 };
            _turnService.SetTurnOrder(turnOrder);

            _combatLoop = new CombatLoopService(
                _eventBus, _turnService, _entityStats, wordData.Resolver, weaponService, _playerId);

            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
            {
                var msg = $"Player turn started (Turn #{evt.TurnNumber}, Round #{evt.RoundNumber})";
                _turnLog.Add(msg);
                Debug.Log($"[CombatLoopScenario] {msg}");
            }));
            _subscriptions.Add(_eventBus.Subscribe<PlayerTurnEndedEvent>(_ =>
            {
                _turnLog.Add("Player turn ended");
                Debug.Log("[CombatLoopScenario] Player turn ended");
            }));
            _subscriptions.Add(_eventBus.Subscribe<GameOverEvent>(evt =>
            {
                _turnLog.Add($"Game over: {evt.PlayerId.Value}");
                Debug.Log($"[CombatLoopScenario] Game over: {evt.PlayerId.Value}");
            }));

            _combatLoop.Start();

            // Submit a word — ember is a valid test word (Damage 1)
            var result = _combatLoop.SubmitWord("ember");
            Debug.Log($"[CombatLoopScenario] SubmitWord('ember') = {result}");

            // In instant mode, animations complete synchronously so turns cycle back to player
            Debug.Log($"[CombatLoopScenario] IsPlayerTurn after submit = {_combatLoop.IsPlayerTurn}");

            // Test invalid word
            var invalidResult = _combatLoop.SubmitWord("xyznotaword");
            Debug.Log($"[CombatLoopScenario] SubmitWord('xyznotaword') = {invalidResult}");

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

            var title = new Label("Combat Loop");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            var statusLabel = new Label($"IsPlayerTurn: {_combatLoop.IsPlayerTurn}  |  IsGameOver: {_combatLoop.IsGameOver}");
            statusLabel.style.fontSize = 16;
            statusLabel.style.color = new Color(0.6f, 0.8f, 1f);
            statusLabel.style.marginBottom = 12;
            root.Add(statusLabel);

            var logTitle = new Label("Turn Log:");
            logTitle.style.fontSize = 16;
            logTitle.style.color = Color.white;
            logTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            logTitle.style.marginBottom = 8;
            root.Add(logTitle);

            foreach (var entry in _turnLog)
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
                new("CombatLoopService created", _combatLoop != null,
                    _combatLoop != null ? null : "CombatLoopService is null"),
                new("Player turn started event fired", _turnLog.Count > 0 && _turnLog[0].Contains("Player turn started"),
                    _turnLog.Count > 0 && _turnLog[0].Contains("Player turn started") ? null : "First event should be PlayerTurnStartedEvent"),
                new("Not game over", !_combatLoop.IsGameOver,
                    !_combatLoop.IsGameOver ? null : "Should not be game over after one word"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _turnLog.Clear();

            (_combatLoop as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _eventBus?.ClearAllSubscriptions();
            _combatLoop = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
