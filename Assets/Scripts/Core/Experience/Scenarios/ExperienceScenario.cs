using System;
using System.Collections.Generic;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Experience.Scenarios
{
    internal sealed class ExperienceScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter EnemyTierParam = new(
            "enemyTier", "Enemy Tier", typeof(int), 2, 1, 5);

        private ExperienceService _service;
        private IEventBus _eventBus;
        private Label _xpLabel;
        private Label _levelLabel;
        private readonly List<IDisposable> _subscriptions = new();

        public ExperienceScenario() : base(new TestScenarioDefinition(
            "experience",
            "Experience & Leveling",
            "Spawns mock enemies and kills them to verify XP gain and level-up events.",
            new[] { EnemyTierParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var tier = ResolveParam<int>(overrides, "enemyTier");
            _eventBus = new EventBus();
            _service = new ExperienceService(_eventBus);

            var adapter = new MockEncounterAdapter();
            var enemyId = new EntityId("test_enemy_0");
            var def = new EntityDefinition("Test Goblin", 20, 5, 0, 0, 0, 0, Color.red,
                new[] { "scratch" }, Tier: tier);
            adapter.Register(enemyId, def);
            _service.SetEncounterService(adapter);

            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;

            _levelLabel = new Label($"Level: {_service.CurrentLevel}");
            _levelLabel.style.fontSize = 24;
            _levelLabel.style.color = Color.white;
            _levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(_levelLabel);

            _xpLabel = new Label($"XP: {_service.CurrentXp}/{_service.XpForNextLevel}");
            _xpLabel.style.fontSize = 20;
            _xpLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            _xpLabel.style.marginTop = 8;
            root.Add(_xpLabel);

            _subscriptions.Add(_eventBus.Subscribe<ExperienceGainedEvent>(evt =>
            {
                _xpLabel.text = $"XP: {evt.TotalXp}/{evt.XpForNextLevel} (+{evt.XpAmount})";
                Debug.Log($"[ExperienceScenario] +{evt.XpAmount} XP (total={evt.TotalXp}/{evt.XpForNextLevel}, lvl={evt.CurrentLevel})");
            }));
            _subscriptions.Add(_eventBus.Subscribe<LevelUpEvent>(evt =>
            {
                _levelLabel.text = $"Level: {evt.NewLevel}";
                Debug.Log($"[ExperienceScenario] LEVEL UP! {evt.PreviousLevel} -> {evt.NewLevel}");
            }));

            _eventBus.Publish(new EntityDiedEvent(enemyId));
            Debug.Log($"[ExperienceScenario] Enemy killed: tier={tier}, hp={def.MaxHealth}");
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var tier = ResolveParam<int>(overrides, "enemyTier");
            int expectedXp = tier * 5 + 20 / 2;

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Service created", _service != null,
                    _service != null ? null : "No service"),
                new("XP gained correctly",
                    _service != null && (_service.CurrentXp > 0 || _service.CurrentLevel > 1),
                    _service != null ? $"XP={_service.CurrentXp}, Level={_service.CurrentLevel}" : "No service"),
                new("XP amount matches formula",
                    _service != null && (_service.CurrentLevel > 1 || _service.CurrentXp == expectedXp),
                    _service != null ? $"Expected {expectedXp}, got XP={_service.CurrentXp}" : "No service"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            (_service as IDisposable)?.Dispose();
            _service = null;
            _eventBus = null;
            _xpLabel = null;
            _levelLabel = null;
        }

        private sealed class MockEncounterAdapter : IEncounterService
        {
            private readonly Dictionary<EntityId, EntityDefinition> _definitions = new();

            public void Register(EntityId id, EntityDefinition def) => _definitions[id] = def;

            public bool IsEncounterActive => true;
            public IReadOnlyList<EntityId> EnemyEntities => new List<EntityId>(_definitions.Keys);
            public EntityId PlayerEntity => new EntityId("player");
            public bool IsEnemy(EntityId id) => _definitions.ContainsKey(id);
            public EntityDefinition GetEntityDefinition(EntityId id) => _definitions[id];
            public void RegisterEnemy(EntityId id, EntityDefinition def) => _definitions[id] = def;
            public void UnregisterEnemy(EntityId id) => _definitions.Remove(id);
            public void StartEncounter(EncounterDefinition e, EntityId p) { }
            public void EndEncounter() { }
        }
    }
}
