using System;
using System.Collections.Generic;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
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
        private static readonly ScenarioParameter TriggerIdParam = new(
            "triggerId", "Trigger ID", typeof(string), "on_round_end");
        private static readonly ScenarioParameter EffectIdParam = new(
            "effectId", "Effect ID", typeof(string), "heal");
        private static readonly ScenarioParameter TargetParam = new(
            "target", "Target", typeof(string), "AllAllies");
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
        private bool _sourceKeyRemoveEquipOk;
        private bool _sourceKeyRemoveAllOk;
        private bool _sourceKeyBothSourcesPresent;
        private readonly List<IDisposable> _subscriptions = new();

        public PassiveScenario() : base(new TestScenarioDefinition(
            "passive-flow",
            "Passive Flow",
            "Tests composable passives with configurable trigger, effect, and target.",
            new[] { TriggerIdParam, EffectIdParam, TargetParam, ValueParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var triggerId = ResolveParam<string>(overrides, "triggerId");
            var effectId = ResolveParam<string>(overrides, "effectId");
            var target = ResolveParam<string>(overrides, "target");
            var value = ResolveParam<int>(overrides, "value");
            _passiveTriggeredCount = 0;

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();
            _turnService = new TurnService(_eventBus);

            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry(null);
            var targetResolver = new PassiveTargetResolver();
            var context = new PassiveContext(_entityStats, _slotService, _eventBus, null);
            _passiveService = new PassiveService(_eventBus, triggerRegistry, effectRegistry, targetResolver, context);

            _structure = new EntityId("structure");
            _ally = new EntityId("ally");

            _entityStats.RegisterEntity(_structure, 40, 0, 0, 8, 4, 0);
            _entityStats.RegisterEntity(_ally, 30, 5, 0, 3, 2, 1);

            _slotService.RegisterAlly(_structure, 0);
            _slotService.RegisterAlly(_ally, 1);

            _turnService.SetTurnOrder(new List<EntityId> { _ally, _structure });

            var passives = new[] { new PassiveEntry(triggerId, null, effectId, null, value, target) };
            _passiveService.RegisterPassives(_structure, passives);

            // Source-keyed registration test: add "equip:ring" source, verify both exist, remove only equip
            var equipPassives = new[] { new PassiveEntry("on_round_start", null, "heal", null, 1, "Self") };
            _passiveService.RegisterPassives(_structure, "equip:ring", equipPassives);
            var allPassives = _passiveService.GetPassives(_structure);
            _sourceKeyBothSourcesPresent = allPassives.Count >= 2;

            _passiveService.RemovePassives(_structure, "equip:ring");
            _sourceKeyRemoveEquipOk = _passiveService.HasPassives(_structure)
                && _passiveService.GetPassives(_structure).Count == passives.Length;

            // Re-add equip passives, then remove all
            _passiveService.RegisterPassives(_structure, "equip:ring", equipPassives);
            _passiveService.RemovePassives(_structure);
            _sourceKeyRemoveAllOk = !_passiveService.HasPassives(_structure);

            // Re-register base passives for the remaining trigger tests
            _passiveService.RegisterPassives(_structure, passives);

            _subscriptions.Add(_eventBus.Subscribe<PassiveTriggeredEvent>(evt =>
            {
                _passiveTriggeredCount++;
                Debug.Log($"[PassiveScenario] Passive triggered: {evt.TriggerId}+{evt.EffectId} from {evt.SourceEntity.Value} " +
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

            var label = new Label($"Passive: {triggerId}+{effectId} → {target} (value={value})");
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

            // Simulate triggers based on type
            if (triggerId == "on_ally_hit")
            {
                _entityStats.ApplyDamage(_ally, 5);
                var newHp = _entityStats.GetCurrentHealth(_ally);
                Debug.Log($"[PassiveScenario] Ally took 5 damage → HP={newHp}");
            }

            if (triggerId == "on_round_end")
            {
                _entityStats.ApplyDamage(_ally, 5);
                _turnService.BeginTurn();
                _turnService.EndTurn();
                _turnService.BeginTurn();
                _turnService.EndTurn();
                var newHp = _entityStats.GetCurrentHealth(_ally);
                Debug.Log($"[PassiveScenario] After round end → Ally HP={newHp}");
            }

            Debug.Log($"[PassiveScenario] Started — trigger={triggerId}, effect={effectId}, " +
                      $"target={target}, value={value}, triggers={_passiveTriggeredCount}");
        }

        private static readonly HashSet<string> SimulatedTriggers = new()
        {
            "on_ally_hit", "on_round_end"
        };

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var triggerId = ResolveParam<string>(overrides, "triggerId");
            var canVerifyTrigger = SimulatedTriggers.Contains(triggerId);
            var triggered = _passiveTriggeredCount > 0;

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
                    !canVerifyTrigger || triggered,
                    !canVerifyTrigger ? null : triggered ? null : "Passive never triggered"),
                new("Source-keyed: both sources present after dual register",
                    _sourceKeyBothSourcesPresent,
                    _sourceKeyBothSourcesPresent ? null : "Expected passives from both sources"),
                new("Source-keyed: remove equip keeps unit passives",
                    _sourceKeyRemoveEquipOk,
                    _sourceKeyRemoveEquipOk ? null : "Removing equip source also removed unit passives"),
                new("Source-keyed: remove all clears everything",
                    _sourceKeyRemoveAllOk,
                    _sourceKeyRemoveAllOk ? null : "RemovePassives(entity) did not clear all"),
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
