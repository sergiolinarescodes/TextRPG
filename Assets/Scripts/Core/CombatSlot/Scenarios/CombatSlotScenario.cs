using System;
using System.Collections.Generic;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.CombatSlot.Scenarios
{
    internal sealed class CombatSlotScenario : DataDrivenScenario
    {
        private ICombatSlotService _slotService;
        private IEventBus _eventBus;
        private readonly List<IDisposable> _subscriptions = new();

        private readonly EntityId _enemyA = new("enemy_a");
        private readonly EntityId _enemyB = new("enemy_b");
        private readonly EntityId _enemyC = new("enemy_c");
        private readonly EntityId _allyA = new("ally_a");

        public CombatSlotScenario() : base(new TestScenarioDefinition(
            "combat-slot",
            "Combat Slot System",
            "Registers entities in enemy/ally slots, verifies lookups, nearest-occupied, and removal.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            _eventBus = new EventBus();
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            _subscriptions.Add(_eventBus.Subscribe<SlotEntityRegisteredEvent>(e =>
                Debug.Log($"[CombatSlotScenario] Registered {e.EntityId.Value} at {e.Slot.Type}[{e.Slot.Index}]")));
            _subscriptions.Add(_eventBus.Subscribe<SlotEntityRemovedEvent>(e =>
                Debug.Log($"[CombatSlotScenario] Removed {e.EntityId.Value} from {e.Slot.Type}[{e.Slot.Index}]")));

            _slotService.RegisterEnemy(_enemyA, 0);
            _slotService.RegisterEnemy(_enemyB, 1);
            _slotService.RegisterEnemy(_enemyC, 2);
            _slotService.RegisterAlly(_allyA, 0);

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

            var title = new Label("Combat Slot System");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            AddInfoRow(root, "Enemy Count", _slotService.GetOccupiedEnemyCount().ToString(), Color.red);
            AddInfoRow(root, "Ally Count", _slotService.GetOccupiedAllyCount().ToString(), Color.green);

            for (int i = 0; i < 3; i++)
            {
                var entity = _slotService.GetEntityAt(SlotType.Enemy, i);
                AddInfoRow(root, $"Enemy Slot {i}",
                    entity.HasValue ? entity.Value.Value : "(empty)",
                    entity.HasValue ? Color.red : Color.gray);
            }

            for (int i = 0; i < 2; i++)
            {
                var entity = _slotService.GetEntityAt(SlotType.Ally, i);
                AddInfoRow(root, $"Ally Slot {i}",
                    entity.HasValue ? entity.Value.Value : "(empty)",
                    entity.HasValue ? Color.green : Color.gray);
            }

            var nearest = _slotService.FindNearestOccupiedSlot(SlotType.Enemy, 1);
            AddInfoRow(root, "Nearest Enemy@1", nearest.HasValue ? nearest.Value.Value : "(none)", Color.yellow);
        }

        private static void AddInfoRow(VisualElement parent, string label, string value, Color valueColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;

            var nameLabel = new Label($"{label}: ");
            nameLabel.style.fontSize = 16;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.width = 200;
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
                new("3 enemies registered", _slotService.GetOccupiedEnemyCount() == 3,
                    _slotService.GetOccupiedEnemyCount() == 3 ? null : $"Expected 3, got {_slotService.GetOccupiedEnemyCount()}"),
                new("1 ally registered", _slotService.GetOccupiedAllyCount() == 1,
                    _slotService.GetOccupiedAllyCount() == 1 ? null : $"Expected 1, got {_slotService.GetOccupiedAllyCount()}"),
                new("Enemy slot 0 = enemy_a",
                    _slotService.GetEntityAt(SlotType.Enemy, 0)?.Value == "enemy_a",
                    _slotService.GetEntityAt(SlotType.Enemy, 0)?.Value == "enemy_a" ? null : "Wrong entity at slot 0"),
                new("Ally slot 0 = ally_a",
                    _slotService.GetEntityAt(SlotType.Ally, 0)?.Value == "ally_a",
                    _slotService.GetEntityAt(SlotType.Ally, 0)?.Value == "ally_a" ? null : "Wrong entity at ally slot 0"),
                new("Nearest enemy from slot 1 = enemy_b",
                    _slotService.FindNearestOccupiedSlot(SlotType.Enemy, 1)?.Value == "enemy_b",
                    _slotService.FindNearestOccupiedSlot(SlotType.Enemy, 1)?.Value == "enemy_b" ? null : "Wrong nearest entity"),
                new("First empty ally slot = 1",
                    _slotService.FindFirstEmptySlot(SlotType.Ally) == 1,
                    _slotService.FindFirstEmptySlot(SlotType.Ally) == 1 ? null : $"Expected 1, got {_slotService.FindFirstEmptySlot(SlotType.Ally)}"),
            };

            // Test removal
            _slotService.RemoveEntity(_enemyB);
            checks.Add(new("Remove enemy_b clears slot 1",
                !_slotService.GetEntityAt(SlotType.Enemy, 1).HasValue,
                !_slotService.GetEntityAt(SlotType.Enemy, 1).HasValue ? null : "Slot 1 still occupied after removal"));
            checks.Add(new("Nearest enemy from slot 1 after removal = enemy_a or enemy_c",
                _slotService.FindNearestOccupiedSlot(SlotType.Enemy, 1).HasValue,
                _slotService.FindNearestOccupiedSlot(SlotType.Enemy, 1).HasValue ? null : "No nearest found"));

            // Re-register for cleanup
            _slotService.RegisterEnemy(_enemyB, 1);

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_slotService as IDisposable)?.Dispose();
            _slotService = null;
            _eventBus = null;
        }
    }
}
