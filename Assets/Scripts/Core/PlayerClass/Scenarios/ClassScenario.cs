using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.EntityStats;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.PlayerClass.Scenarios
{
    internal sealed class ClassScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter ClassParam = new(
            "playerClass", "Player Class (0=Mage, 1=Warrior, 2=Merchant)", typeof(int), 0, 0, 2);

        private IEventBus _eventBus;
        private IEntityStatsService _entityStats;
        private ClassService _classService;
        private EntityId _playerId;
        private int _warriorModifiedDamage;
        private readonly List<IDisposable> _subscriptions = new();

        public ClassScenario() : base(new TestScenarioDefinition(
            "player-class",
            "Player Class System",
            "Displays class stats, verifies Warrior damage modifier, and class-specific passive descriptions.",
            new[] { ClassParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var classIndex = ResolveParam<int>(overrides, "playerClass");
            var selectedClass = (PlayerClass)classIndex;
            var classDef = ClassDefinitions.Get(selectedClass);

            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _playerId = new EntityId("test-player");

            var tagResolver = new TestTagResolver();
            _classService = new ClassService(_eventBus, selectedClass, _playerId, tagResolver);

            PlayerDefaults.Register(_entityStats, _playerId, classDef);

            // Test Warrior damage modifier (MELEE tag → 50% boost)
            _warriorModifiedDamage = _classService.ModifyValue(
                ActionNames.Damage, 10, "punch", _playerId);

            _subscriptions.Add(_eventBus.Subscribe<ClassScrollLearnedEvent>(evt =>
                Debug.Log($"[ClassScenario] Mage scroll learned at level {evt.Level}: {evt.SpellWord}")));

            BuildUI(classDef);
            Debug.Log($"[ClassScenario] Created {classDef.DisplayName} (HP={classDef.MaxHealth}, STR={classDef.Strength}, MGC={classDef.MagicPower})");
        }

        private void BuildUI(ClassDefinition def)
        {
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            root.style.paddingTop = 20;
            root.style.paddingLeft = 20;

            var title = new Label(def.DisplayName);
            title.style.fontSize = 28;
            title.style.color = def.Color;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(title);

            var desc = new Label(def.Description);
            desc.style.fontSize = 14;
            desc.style.color = new Color(0.7f, 0.7f, 0.7f);
            desc.style.marginTop = 4;
            desc.style.marginBottom = 12;
            root.Add(desc);

            AddStatRow(root, "HP", def.MaxHealth, Color.green);
            AddStatRow(root, "Strength", def.Strength, new Color(1f, 0.4f, 0.3f));
            AddStatRow(root, "Magic Power", def.MagicPower, new Color(0.6f, 0.4f, 1f));
            AddStatRow(root, "Physical Def", def.PhysicalDefense, new Color(0.8f, 0.8f, 0.8f));
            AddStatRow(root, "Magic Def", def.MagicDefense, new Color(0.5f, 0.7f, 1f));
            AddStatRow(root, "Luck", def.Luck, new Color(1f, 0.85f, 0.2f));
            AddStatRow(root, "Max Mana", def.MaxMana, new Color(0.3f, 0.6f, 1f));
            AddStatRow(root, "Mana Regen", def.ManaRegen, new Color(0.3f, 0.6f, 1f));
            AddStatRow(root, "Constitution", def.Constitution, new Color(0.9f, 0.6f, 0.3f));

            if (def.PassiveDescriptions is { Length: > 0 })
            {
                var passiveHeader = new Label("Passives:");
                passiveHeader.style.fontSize = 16;
                passiveHeader.style.color = Color.white;
                passiveHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                passiveHeader.style.marginTop = 16;
                root.Add(passiveHeader);

                foreach (var passive in def.PassiveDescriptions)
                {
                    var p = new Label($"  {passive}");
                    p.style.fontSize = 14;
                    p.style.color = new Color(1f, 0.85f, 0.2f);
                    p.style.marginTop = 2;
                    root.Add(p);
                }
            }

            // Show Warrior modifier test result
            var modLabel = new Label($"Warrior modifier test: Damage(10) on MELEE word = {_warriorModifiedDamage}");
            modLabel.style.fontSize = 14;
            modLabel.style.color = new Color(0.6f, 0.8f, 0.6f);
            modLabel.style.marginTop = 16;
            root.Add(modLabel);
        }

        private static void AddStatRow(VisualElement parent, string label, int value, Color color)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 4;

            var nameLabel = new Label($"{label}:");
            nameLabel.style.width = 120;
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = Color.white;
            row.Add(nameLabel);

            var valueLabel = new Label(value.ToString());
            valueLabel.style.fontSize = 14;
            valueLabel.style.color = color;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var classIndex = ResolveParam<int>(overrides, "playerClass");
            var selectedClass = (PlayerClass)classIndex;
            var def = ClassDefinitions.Get(selectedClass);

            bool isWarrior = selectedClass == PlayerClass.Warrior;
            int expectedModified = isWarrior ? 15 : 10;

            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Class service created", _classService != null,
                    _classService != null ? null : "ClassService is null"),
                new("Correct class selected", _classService?.SelectedClass == selectedClass,
                    _classService?.SelectedClass == selectedClass ? null
                        : $"Expected {selectedClass}, got {_classService?.SelectedClass}"),
                new("Player entity registered", _entityStats?.HasEntity(_playerId) == true,
                    _entityStats?.HasEntity(_playerId) == true ? null : "Player not registered"),
                new("MaxHealth matches class", _entityStats?.GetStat(_playerId, StatType.MaxHealth) == def.MaxHealth,
                    _entityStats?.GetStat(_playerId, StatType.MaxHealth) == def.MaxHealth ? null
                        : $"Expected {def.MaxHealth}, got {_entityStats?.GetStat(_playerId, StatType.MaxHealth)}"),
                new("Strength matches class", _entityStats?.GetStat(_playerId, StatType.Strength) == def.Strength,
                    _entityStats?.GetStat(_playerId, StatType.Strength) == def.Strength ? null
                        : $"Expected {def.Strength}, got {_entityStats?.GetStat(_playerId, StatType.Strength)}"),
                new("Warrior modifier correct", _warriorModifiedDamage == expectedModified,
                    _warriorModifiedDamage == expectedModified ? null
                        : $"Expected {expectedModified}, got {_warriorModifiedDamage}"),
            };

            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            (_classService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            _classService = null;
            _entityStats = null;
            _eventBus = null;
        }

        private sealed class TestTagResolver : WordAction.IWordTagResolver
        {
            public IReadOnlyList<string> GetTags(string word) => Array.Empty<string>();
            public IReadOnlyList<string> GetWordsByTag(string tag) => Array.Empty<string>();
            public string GetRandomWordByTag(string tag) => null;
            public bool HasTag(string word, string tag) => tag == "MELEE";
            public void AddTag(string word, string tag) { }
            public IEnumerable<string> AllTags => Array.Empty<string>();
        }
    }
}
