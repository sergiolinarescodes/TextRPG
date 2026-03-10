using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Encounter.Scenarios
{
    internal sealed class EncounterScenario : DataDrivenScenario
    {
        private IEncounterService _encounterService;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private ICombatSlotService _slotService;
        private IEventBus _eventBus;
        private EntityId _player;
        private bool _encounterStarted;
        private int _enemyCount;
        private readonly List<IDisposable> _subscriptions = new();

        public EncounterScenario() : base(new TestScenarioDefinition(
            "encounter-flow",
            "Encounter Flow",
            "Starts an ORC encounter, registers entities in slots, verifies turn order and enemy spawning.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _encounterStarted = false;
            _enemyCount = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);
            _slotService = new CombatSlotService(_eventBus);
            var combatContext = new CombatContext();
            var enemyResolver = new EnemyWordResolver();

            _encounterService = new EncounterService(_eventBus, _entityStats, _turnService, _slotService, combatContext, enemyResolver);

            _player = new EntityId("player");
            _entityStats.RegisterEntity(_player, maxHealth: 100, strength: 12, magicPower: 8,
                physicalDefense: 6, magicDefense: 4, luck: 5);

            _subscriptions.Add(_eventBus.Subscribe<EncounterStartedEvent>(e =>
            {
                _encounterStarted = true;
                _enemyCount = e.EnemyCount;
                Debug.Log($"[EncounterScenario] Encounter started: {e.EncounterId} with {e.EnemyCount} enemies");
            }));
            _subscriptions.Add(_eventBus.Subscribe<EnemySpawnedEvent>(e =>
                Debug.Log($"[EncounterScenario] Enemy spawned: {e.EnemyName} ({e.EntityId.Value})")));
            _subscriptions.Add(_eventBus.Subscribe<EncounterEndedEvent>(e =>
                Debug.Log($"[EncounterScenario] Encounter ended: victory={e.Victory}")));

            var orcDef = UnitIds.Enemies.ORC_DEF;

            var encounter = new EncounterDefinition(
                "orc_ambush", "Orc Ambush",
                new[] { orcDef, orcDef }
            );

            _encounterService.StartEncounter(encounter, _player);
            UnitDatabaseLoader.RegisterUnitWords(enemyResolver, UnitIds.Enemies.ORC);

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

            var title = new Label("Encounter Flow");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Active", _encounterService.IsEncounterActive.ToString(), new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "Enemies", _enemyCount.ToString(), new Color(1f, 0.3f, 0.3f));
            AddInfoRow(root, "Turn Entity", _turnService.CurrentEntity.Value, new Color(0.6f, 0.8f, 1f));

            var enemiesTitle = new Label("Enemy Entities:");
            enemiesTitle.style.fontSize = 16;
            enemiesTitle.style.color = Color.white;
            enemiesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            enemiesTitle.style.marginTop = 12;
            enemiesTitle.style.marginBottom = 8;
            root.Add(enemiesTitle);

            foreach (var enemy in _encounterService.EnemyEntities)
            {
                var def = _encounterService.GetEntityDefinition(enemy);
                var hp = _entityStats.GetCurrentHealth(enemy);
                var slot = _slotService.GetSlot(enemy);
                var slotStr = slot.HasValue ? $"Slot {slot.Value.Type}[{slot.Value.Index}]" : "no slot";
                var label = new Label($"  {def.Name} ({enemy.Value}) HP={hp} @ {slotStr}");
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
                new("Enemies spawned", _enemyCount == 2,
                    _enemyCount == 2 ? null : $"Expected 2 enemies, got {_enemyCount}"),
                new("Encounter is active", _encounterService.IsEncounterActive,
                    _encounterService.IsEncounterActive ? null : "Encounter is not active"),
                new("Enemy entities registered", _encounterService.EnemyEntities.Count == 2,
                    _encounterService.EnemyEntities.Count == 2 ? null : $"Expected 2 enemy entities, got {_encounterService.EnemyEntities.Count}"),
                new("Player is first in turn order", _turnService.CurrentEntity.Equals(_player),
                    _turnService.CurrentEntity.Equals(_player) ? null : $"Expected player first, got {_turnService.CurrentEntity.Value}"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_encounterService as IDisposable)?.Dispose();
            (_slotService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _encounterService = null;
            _slotService = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
