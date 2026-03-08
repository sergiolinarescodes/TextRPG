using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Passive.Scenarios
{
    internal sealed class PassiveScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter PassiveIdParam = new(
            "passiveId", "Passive ID", typeof(string), "heal_on_ally_hit");
        private static readonly ScenarioParameter ValueParam = new(
            "value", "Value", typeof(int), 2, 1, 10);

        private IPassiveService _passiveService;
        private IEntityStatsService _entityStats;
        private ICombatSlotService _slotService;
        private ITurnService _turnService;
        private IEventBus _eventBus;
        private EntityId _structure;
        private EntityId _ally;
        private int _passiveTriggeredCount;
        private readonly List<IDisposable> _subscriptions = new();

        public PassiveScenario() : base(new TestScenarioDefinition(
            "passive-flow",
            "Passive Flow",
            "Registers passives on a structure entity, simulates events, and verifies passive triggers.",
            new[] { PassiveIdParam, ValueParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var passiveId = ResolveParam<string>(overrides, "passiveId");
            var value = ResolveParam<int>(overrides, "value");
            _passiveTriggeredCount = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();
            _turnService = new TurnService(_eventBus);

            var handlerRegistry = PassiveSystemInstaller.CreateHandlerRegistry();
            var context = new PassiveContext(_entityStats, _slotService, _eventBus, null);
            _passiveService = new PassiveService(_eventBus, handlerRegistry, context);

            _structure = new EntityId("structure");
            _ally = new EntityId("ally");

            _entityStats.RegisterEntity(_structure, 40, 0, 0, 8, 4, 0);
            _entityStats.RegisterEntity(_ally, 30, 5, 0, 3, 2, 1);

            _slotService.RegisterAlly(_structure, 0);
            _slotService.RegisterAlly(_ally, 1);

            _turnService.SetTurnOrder(new List<EntityId> { _ally, _structure });

            var passives = new[] { new Encounter.PassiveEntry(passiveId, value) };
            _passiveService.RegisterPassives(_structure, passives);

            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(evt =>
            {
                _passiveTriggeredCount++;
                Debug.Log($"[PassiveScenario] Passive triggered: {evt.PassiveId} from {evt.SourceEntity.Value} " +
                          $"value={evt.Value} affected={evt.AffectedEntity?.Value ?? "none"}");
            }));

            // Build simple visual
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = Color.black;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            var label = new Label($"Passive: {passiveId} (value={value})");
            label.style.color = Color.white;
            label.style.fontSize = 24;
            root.Add(label);

            var structLabel = new Label($"Structure: {_structure.Value} HP={_entityStats.GetCurrentHealth(_structure)}");
            structLabel.style.color = Color.cyan;
            structLabel.style.fontSize = 20;
            root.Add(structLabel);

            var allyLabel = new Label($"Ally: {_ally.Value} HP={_entityStats.GetCurrentHealth(_ally)}");
            allyLabel.style.color = Color.green;
            allyLabel.style.fontSize = 20;
            root.Add(allyLabel);

            // Simulate damage to ally to test heal_on_ally_hit
            if (passiveId == "heal_on_ally_hit")
            {
                _entityStats.ApplyDamage(_ally, 5);
                var newHp = _entityStats.GetCurrentHealth(_ally);
                Debug.Log($"[PassiveScenario] Ally took 5 damage → HP={newHp} (should be healed by {value})");
            }

            // Simulate round end to test heal_on_round_end
            if (passiveId == "heal_on_round_end")
            {
                _entityStats.ApplyDamage(_ally, 5);
                _turnService.BeginTurn();
                _turnService.EndTurn();
                _turnService.BeginTurn();
                _turnService.EndTurn();
                var newHp = _entityStats.GetCurrentHealth(_ally);
                Debug.Log($"[PassiveScenario] After round end → Ally HP={newHp}");
            }

            Debug.Log($"[PassiveScenario] Started — passive={passiveId}, value={value}, " +
                      $"triggers={_passiveTriggeredCount}");
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Structure entity registered",
                    _entityStats.HasEntity(_structure),
                    _entityStats.HasEntity(_structure) ? null : "Structure not registered"),
                new("Ally entity registered",
                    _entityStats.HasEntity(_ally),
                    _entityStats.HasEntity(_ally) ? null : "Ally not registered"),
                new("Passives registered on structure",
                    _passiveService.HasPassives(_structure),
                    _passiveService.HasPassives(_structure) ? null : "No passives on structure"),
                new("Passive triggered at least once",
                    _passiveTriggeredCount > 0,
                    _passiveTriggeredCount > 0 ? null : "Passive never triggered"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            _eventBus?.ClearAllSubscriptions();
            _passiveService = null;
            _entityStats = null;
            _slotService = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
