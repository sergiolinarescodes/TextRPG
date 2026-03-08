using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Scoring;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.CombatAI.Scenarios
{
    internal sealed class CombatAIScenario : DataDrivenScenario
    {
        private ICombatAIService _aiService;
        private IEncounterService _encounterService;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private ICombatSlotService _slotService;
        private IEventBus _eventBus;
        private EntityId _player;
        private int _aiActionsExecuted;
        private readonly List<IDisposable> _subscriptions = new();

        public CombatAIScenario() : base(new TestScenarioDefinition(
            "combat-ai-flow",
            "Combat AI Flow",
            "Starts an encounter, simulates AI turns with scoring for enemies and summons, verifies abilities are chosen and executed.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _aiActionsExecuted = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);
            _slotService = new CombatSlotService(_eventBus);
            var combatContext = new CombatContext();
            var enemyResolver = new EnemyWordResolver();

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            var statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            _encounterService = new EncounterService(_eventBus, _entityStats, _turnService, _slotService, combatContext, enemyResolver);

            var actionHandlerRegistry = ActionExecutionTestFactory.CreateHandlerRegistry(_eventBus, _entityStats, statusEffects, combatContext);
            var wordData = WordActionTestFactory.CreateTestData();

            var compositeResolver = new CompositeWordResolver(wordData.Resolver, enemyResolver);
            var actionExecution = new ActionExecutionService(_eventBus, compositeResolver, actionHandlerRegistry, combatContext);

            var scorers = CombatAISystemInstaller.CreateScorerRegistry(statusEffects);
            _aiService = new CombatAIService(_eventBus, _encounterService, _entityStats, _turnService, _slotService, combatContext, actionExecution, scorers, enemyResolver);

            _player = new EntityId("player");
            _entityStats.RegisterEntity(_player, maxHealth: 100, strength: 12, magicPower: 8,
                physicalDefense: 6, magicDefense: 4, luck: 5);

            var orcDef = UnitIds.Enemies.ORC_DEF;
            var encounter = new EncounterDefinition(
                "orc_test", "Orc Test",
                new[] { orcDef }
            );

            _subscriptions.Add(_eventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                _aiActionsExecuted++;
                Debug.Log($"[CombatAIScenario] AI executed word: {e.Word}");
            }));

            _encounterService.StartEncounter(encounter, _player);
            UnitDatabaseLoader.RegisterUnitWords(enemyResolver, UnitIds.Enemies.ORC);

            _turnService.BeginTurn();
            _turnService.EndTurn();
            _turnService.BeginTurn();
            _turnService.EndTurn();

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

            var title = new Label("Combat AI Flow");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "AI Actions", _aiActionsExecuted.ToString(), new Color(1f, 0.85f, 0.3f));
            AddInfoRow(root, "Player HP", $"{_entityStats.GetCurrentHealth(_player)}/100", new Color(0.2f, 0.8f, 0.2f));

            foreach (var enemy in _encounterService.EnemyEntities)
            {
                if (_entityStats.HasEntity(enemy))
                {
                    var slot = _slotService.GetSlot(enemy);
                    var slotStr = slot.HasValue ? $"Slot {slot.Value.Index}" : "no slot";
                    AddInfoRow(root, $"Enemy {enemy.Value}", $"HP={_entityStats.GetCurrentHealth(enemy)} @ {slotStr}", Color.red);
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
                new("AI service initialized", _aiService != null,
                    _aiService != null ? null : "AI service is null"),
                new("Encounter active", _encounterService.IsEncounterActive,
                    _encounterService.IsEncounterActive ? null : "Encounter not active"),
                new("AI executed actions", _aiActionsExecuted > 0,
                    _aiActionsExecuted > 0 ? null : "AI did not execute any actions"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_aiService as IDisposable)?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            (_slotService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _aiService = null;
            _encounterService = null;
            _slotService = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
