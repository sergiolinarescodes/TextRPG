using System;
using System.Collections.Generic;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Modifier;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextRPG.Core.EntityStats.Scenarios
{
    internal sealed class EntityStatsScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter MaxHealthParam = new(
            "maxHealth", "Max Health", typeof(int), 100, 10, 500);
        private static readonly ScenarioParameter DamageAmountParam = new(
            "damageAmount", "Damage Amount", typeof(int), 25, 0, 200);
        private static readonly ScenarioParameter HealAmountParam = new(
            "healAmount", "Heal Amount", typeof(int), 10, 0, 200);

        private IEntityStatsService _service;
        private IEventBus _eventBus;
        private EntityId _testEntity;
        private int _maxHealth;
        private int _damageAmount;
        private int _healAmount;
        private readonly List<IDisposable> _subscriptions = new();

        public EntityStatsScenario() : base(new TestScenarioDefinition(
            "entity-stats",
            "Entity Stats",
            "Registers an entity, applies damage and healing, adds a stat modifier, and displays stat bars.",
            new[] { MaxHealthParam, DamageAmountParam, HealAmountParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _maxHealth = ResolveParam<int>(overrides, "maxHealth");
            _damageAmount = ResolveParam<int>(overrides, "damageAmount");
            _healAmount = ResolveParam<int>(overrides, "healAmount");

            _eventBus = new EventBus();
            _service = new EntityStatsService(_eventBus);
            _testEntity = new EntityId("test-hero");

            _subscriptions.Add(_eventBus.Subscribe<EntityRegisteredEvent>(e =>
                Debug.Log($"[EntityStatsScenario] Registered '{e.EntityId.Value}' with MaxHealth={e.MaxHealth}")));
            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(e =>
                Debug.Log($"[EntityStatsScenario] '{e.EntityId.Value}' took {e.Amount} damage, HP={e.RemainingHealth}")));
            _subscriptions.Add(_eventBus.Subscribe<HealedEvent>(e =>
                Debug.Log($"[EntityStatsScenario] '{e.EntityId.Value}' healed {e.Amount}, HP={e.NewHealth}")));
            _subscriptions.Add(_eventBus.Subscribe<EntityDiedEvent>(e =>
                Debug.Log($"[EntityStatsScenario] '{e.EntityId.Value}' died!")));
            _subscriptions.Add(_eventBus.Subscribe<StatModifierAddedEvent>(e =>
                Debug.Log($"[EntityStatsScenario] Modifier '{e.ModifierId}' added to {e.Stat} on '{e.EntityId.Value}'")));

            _service.RegisterEntity(_testEntity, _maxHealth, strength: 10, magicPower: 8,
                                    physicalDefense: 5, magicDefense: 4, luck: 3);
            _service.ApplyDamage(_testEntity, _damageAmount);
            _service.ApplyHeal(_testEntity, _healAmount);
            _service.AddModifier(_testEntity, StatType.Strength, new FlatModifier("str-buff", 5));

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

            var title = new Label("Entity Stats");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            var entityLabel = new Label($"Entity: {_testEntity.Value}");
            entityLabel.style.fontSize = 18;
            entityLabel.style.color = new Color(0.6f, 0.8f, 1f);
            entityLabel.style.marginBottom = 12;
            root.Add(entityLabel);

            AddStatBar(root, "Health", _service.GetCurrentHealth(_testEntity),
                       _service.GetStat(_testEntity, StatType.MaxHealth), new Color(0.2f, 0.8f, 0.2f));

            var stats = new[] { StatType.Strength, StatType.MagicPower, StatType.PhysicalDefense, StatType.MagicDefense, StatType.Luck };
            var colors = new[]
            {
                new Color(1f, 0.4f, 0.2f),
                new Color(0.5f, 0.3f, 1f),
                new Color(0.6f, 0.6f, 0.7f),
                new Color(0.4f, 0.6f, 0.9f),
                new Color(1f, 0.85f, 0.3f)
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var baseStat = _service.GetBaseStat(_testEntity, stats[i]);
                var effective = _service.GetStat(_testEntity, stats[i]);
                var label = $"{stats[i]} (base {baseStat})";
                AddStatBar(root, label, effective, 50, colors[i]);
            }
        }

        private static void AddStatBar(VisualElement parent, string label, int current, int max, Color color)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;

            var nameLabel = new Label(label);
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = Color.white;
            nameLabel.style.width = 200;
            row.Add(nameLabel);

            var barContainer = new VisualElement();
            barContainer.style.flexGrow = 1;
            barContainer.style.height = 18;
            barContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
            barContainer.style.borderTopLeftRadius = 4;
            barContainer.style.borderTopRightRadius = 4;
            barContainer.style.borderBottomLeftRadius = 4;
            barContainer.style.borderBottomRightRadius = 4;
            row.Add(barContainer);

            var pct = max > 0 ? (float)current / max * 100f : 0f;
            var bar = new VisualElement();
            bar.style.width = Length.Percent(Math.Min(pct, 100f));
            bar.style.height = Length.Percent(100);
            bar.style.backgroundColor = color;
            bar.style.borderTopLeftRadius = 4;
            bar.style.borderTopRightRadius = 4;
            bar.style.borderBottomLeftRadius = 4;
            bar.style.borderBottomRightRadius = 4;
            barContainer.Add(bar);

            var valueLabel = new Label($"  {current}/{max}");
            valueLabel.style.fontSize = 14;
            valueLabel.style.color = Color.white;
            valueLabel.style.width = 60;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var expectedHealth = Math.Max(0, _maxHealth - _damageAmount);
            expectedHealth = Math.Min(_maxHealth, expectedHealth + _healAmount);

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("Service initialized", _service != null,
                    _service != null ? null : "Service is null"),
                new("Entity registered", _service.HasEntity(_testEntity),
                    _service.HasEntity(_testEntity) ? null : "Entity not found"),
                new("Health correct after damage+heal", _service.GetCurrentHealth(_testEntity) == expectedHealth,
                    _service.GetCurrentHealth(_testEntity) == expectedHealth
                        ? null : $"Expected {expectedHealth} but got {_service.GetCurrentHealth(_testEntity)}"),
                new("Strength modifier applied", _service.GetStat(_testEntity, StatType.Strength) == 15,
                    _service.GetStat(_testEntity, StatType.Strength) == 15
                        ? null : $"Expected 15 but got {_service.GetStat(_testEntity, StatType.Strength)}"),
                new("Base strength unchanged", _service.GetBaseStat(_testEntity, StatType.Strength) == 10,
                    _service.GetBaseStat(_testEntity, StatType.Strength) == 10
                        ? null : $"Expected 10 but got {_service.GetBaseStat(_testEntity, StatType.Strength)}"),
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
        }

        private sealed class FlatModifier : IModifier<int>
        {
            public string Id { get; }
            public int Priority => 0;
            public bool IsActive => true;
            private readonly int _amount;

            public FlatModifier(string id, int amount)
            {
                Id = id;
                _amount = amount;
            }

            public int Apply(int value) => value + _amount;
        }
    }
}
