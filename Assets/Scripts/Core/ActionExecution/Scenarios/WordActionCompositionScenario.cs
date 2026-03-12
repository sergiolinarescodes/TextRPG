using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.WordAction;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.ActionExecution.Scenarios
{
    internal sealed class WordActionCompositionScenario : DataDrivenScenario
    {
        private static readonly string[] TestWords =
        {
            "sear", "mend", "scorch", "deluge", "strike", "terrorize",
            "ignite", "zap", "rally", "snipe", "purge", "conjure", "barrage"
        };

        private IActionExecutionService _executionService;
        private IEntityStatsService _entityStats;
        private ITurnService _turnService;
        private IStatusEffectService _statusEffects;
        private ICombatSlotService _slotService;
        private IEventBus _eventBus;

        private EntityId _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA;

        private int _wordsCompleted;
        private readonly List<string> _executedWords = new();
        private readonly Dictionary<string, int> _handlerCounts = new();
        private int _totalHandlers;
        private readonly List<DamageTakenEvent> _damageEvents = new();
        private readonly List<HealedEvent> _healEvents = new();
        private readonly List<StatusEffectAppliedEvent> _statusApplied = new();
        private readonly List<PushActionEvent> _pushEvents = new();
        private bool _summonDetected;
        private readonly List<IDisposable> _subscriptions = new();

        public WordActionCompositionScenario() : base(new TestScenarioDefinition(
            "word-action-composition",
            "Word-Action Composition Test",
            "Executes 13 composed words covering all 10 action handlers with various target types, area shapes, and status interactions.",
            Array.Empty<ScenarioParameter>()
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            ResetState();
            SetupServices();
            SetupEntities();
            RegisterTestWords();
            SubscribeEvents();

            foreach (var word in TestWords)
            {
                _executionService.ExecuteWord(word);
            }

            BuildUI();
        }

        private void ResetState()
        {
            _wordsCompleted = 0;
            _executedWords.Clear();
            _handlerCounts.Clear();
            _totalHandlers = 0;
            _damageEvents.Clear();
            _healEvents.Clear();
            _statusApplied.Clear();
            _pushEvents.Clear();
            _summonDetected = false;
        }

        private void SetupServices()
        {
            _eventBus = new EventBus();
            _entityStats = new EntityStatsService(_eventBus);
            _turnService = new TurnService(_eventBus);

            _slotService = new CombatSlotService(_eventBus);
            _slotService.Initialize();

            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(_entityStats, _turnService, _eventBus);
            _statusEffects = new StatusEffectService(_eventBus, _entityStats, _turnService, effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)_statusEffects;

            var combatContext = new CombatContext();

            _hero = new EntityId("hero");
            _enemyA = new EntityId("enemy_a");
            _enemyB = new EntityId("enemy_b");
            _enemyC = new EntityId("enemy_c");
            _enemyD = new EntityId("enemy_d");
            _allyA = new EntityId("ally_a");

            var enemies = new[] { _enemyA, _enemyB, _enemyC, _enemyD };
            var allies = new[] { _allyA };

            combatContext.SetSourceEntity(_hero);
            combatContext.SetEnemies(enemies);
            combatContext.SetAllies(allies);
            combatContext.SetSlotService(_slotService);
            combatContext.SetEntityStats(_entityStats);
            combatContext.SetStatusEffects(_statusEffects);

            var actionHandlerRegistry = ActionExecutionTestFactory.CreateHandlerRegistry(
                _eventBus, _entityStats, _statusEffects, combatContext, _turnService);

            var resolver = new EnemyWordResolver();
            _executionService = new ActionExecutionService(_eventBus, resolver, actionHandlerRegistry, combatContext);

            RegisterTestWordsOn(resolver);
        }

        private void SetupEntities()
        {
            _entityStats.RegisterEntity(_hero, maxHealth: 100, strength: 12, magicPower: 8,
                physicalDefense: 5, magicDefense: 4, luck: 3);
            _entityStats.RegisterEntity(_enemyA, maxHealth: 80, strength: 8, magicPower: 6,
                physicalDefense: 4, magicDefense: 3, luck: 2);
            _entityStats.RegisterEntity(_enemyB, maxHealth: 60, strength: 6, magicPower: 4,
                physicalDefense: 3, magicDefense: 2, luck: 1);
            _entityStats.RegisterEntity(_enemyC, maxHealth: 100, strength: 10, magicPower: 8,
                physicalDefense: 6, magicDefense: 5, luck: 4);
            _entityStats.RegisterEntity(_enemyD, maxHealth: 40, strength: 4, magicPower: 3,
                physicalDefense: 2, magicDefense: 1, luck: 1);
            _entityStats.RegisterEntity(_allyA, maxHealth: 70, strength: 7, magicPower: 5,
                physicalDefense: 4, magicDefense: 3, luck: 2);

            var allEntities = new[] { _hero, _enemyA, _enemyB, _enemyC, _enemyD, _allyA };
            _turnService.SetTurnOrder(allEntities);

            _slotService.RegisterEnemy(_enemyA, 0);
            _slotService.RegisterEnemy(_enemyB, 1);
            _slotService.RegisterEnemy(_enemyC, 2);
        }

        private void RegisterTestWords() { }

        private static void RegisterTestWordsOn(EnemyWordResolver resolver)
        {
            // 1. sear — Damage(3), SingleEnemy, Single
            resolver.RegisterWord("sear",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));

            // 2. mend — Heal(5), Self, Single
            resolver.RegisterWord("mend",
                new List<WordActionMapping> { new("Heal", 5) },
                new WordMeta("Self", 0, 0, AreaShape.Single));

            // 3. scorch — Damage(2)+Burn(3), AreaEnemies, Single
            resolver.RegisterWord("scorch",
                new List<WordActionMapping> { new("Damage", 2), new("Burn", 3) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Single));

            // 4. deluge — Water(4)+Damage(2)+Push(2), AreaAll, Cross
            resolver.RegisterWord("deluge",
                new List<WordActionMapping> { new("Water", 4), new("Damage", 2), new("Push", 2) },
                new WordMeta("AreaAll", 0, 0, AreaShape.Cross));

            // 5. strike — Damage(4)+Stun(2), Melee, Single
            resolver.RegisterWord("strike",
                new List<WordActionMapping> { new("Damage", 4), new("Stun", 2) },
                new WordMeta("Melee", 1, 0, AreaShape.Single));

            // 6. terrorize — Fear(3)+Damage(1), RandomEnemy, Single
            resolver.RegisterWord("terrorize",
                new List<WordActionMapping> { new("Fear", 3), new("Damage", 1) },
                new WordMeta("RandomEnemy", 0, 0, AreaShape.Single));

            // 7. ignite — Burn(3), AreaEnemies, Diamond2
            resolver.RegisterWord("ignite",
                new List<WordActionMapping> { new("Burn", 3) },
                new WordMeta("AreaEnemies", 0, 0, AreaShape.Diamond2));

            // 8. zap — Shock(4), SingleEnemy, Single
            resolver.RegisterWord("zap",
                new List<WordActionMapping> { new("Shock", 4) },
                new WordMeta("SingleEnemy", 0, 0, AreaShape.Single));

            // 9. rally — Heal(3), AllAlliesAndSelf, Single
            resolver.RegisterWord("rally",
                new List<WordActionMapping> { new("Heal", 3) },
                new WordMeta("AllAlliesAndSelf", 0, 0, AreaShape.Single));

            // 10. snipe — Damage(5), LowestHealthEnemy, Single
            resolver.RegisterWord("snipe",
                new List<WordActionMapping> { new("Damage", 5) },
                new WordMeta("LowestHealthEnemy", 0, 0, AreaShape.Single));

            // 11. purge — Damage(3), AllBurningEnemies, Single
            resolver.RegisterWord("purge",
                new List<WordActionMapping> { new("Damage", 3) },
                new WordMeta("AllEnemies+Burning", 0, 0, AreaShape.Single));

            // 12. conjure — Summon(4), Self, Single
            resolver.RegisterWord("conjure",
                new List<WordActionMapping> { new("Summon", 4) },
                new WordMeta("Self", 0, 0, AreaShape.Single));

            // 13. barrage — Damage(2)+Push(1), TwoRandomEnemies, Square3x3
            resolver.RegisterWord("barrage",
                new List<WordActionMapping> { new("Damage", 2), new("Push", 1) },
                new WordMeta("TwoRandomEnemies", 0, 0, AreaShape.Square3x3));
        }

        private void SubscribeEvents()
        {
            _subscriptions.Add(_eventBus.Subscribe<ActionExecutionCompletedEvent>(e =>
            {
                _wordsCompleted++;
                _executedWords.Add(e.Word);
                Debug.Log($"[CompositionScenario] Word completed: {e.Word} ({_wordsCompleted}/13)");
            }));

            _subscriptions.Add(_eventBus.Subscribe<ActionHandlerExecutedEvent>(e =>
            {
                _totalHandlers++;
                if (!_handlerCounts.ContainsKey(e.ActionId))
                    _handlerCounts[e.ActionId] = 0;
                _handlerCounts[e.ActionId]++;
                Debug.Log($"[CompositionScenario] Handler: {e.ActionId}(value={e.Value}) on {e.Targets.Count} targets");
            }));

            _subscriptions.Add(_eventBus.Subscribe<DamageTakenEvent>(e =>
            {
                _damageEvents.Add(e);
                Debug.Log($"[CompositionScenario] Damage: {e.EntityId.Value} took {e.Amount}, HP={e.RemainingHealth}");
            }));

            _subscriptions.Add(_eventBus.Subscribe<HealedEvent>(e =>
            {
                _healEvents.Add(e);
                Debug.Log($"[CompositionScenario] Heal: {e.EntityId.Value} +{e.Amount}, HP={e.NewHealth}");
            }));

            _subscriptions.Add(_eventBus.Subscribe<StatusEffectAppliedEvent>(e =>
            {
                _statusApplied.Add(e);
                Debug.Log($"[CompositionScenario] Status: {e.Type} on {e.Target.Value} {(e.Duration < 0 ? "permanently" : $"for {e.Duration} turns")}");
            }));

            _subscriptions.Add(_eventBus.Subscribe<PushActionEvent>(e =>
            {
                _pushEvents.Add(e);
                Debug.Log($"[CompositionScenario] Push: {e.Source.Value} -> {e.Target.Value} (force={e.Value})");
            }));

            _subscriptions.Add(_eventBus.Subscribe<EntityRegisteredEvent>(e =>
            {
                if (e.EntityId.Value.StartsWith("summon"))
                {
                    _summonDetected = true;
                    Debug.Log($"[CompositionScenario] Summon detected: {e.EntityId.Value}");
                }
            }));
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

            var title = new Label("Word-Action Composition Test");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 16;
            root.Add(title);

            // Execution Summary
            AddSectionHeader(root, "Execution Summary");
            AddInfoRow(root, "Words Completed", $"{_wordsCompleted}/13", new Color(0.6f, 0.8f, 1f));
            AddInfoRow(root, "Total Handlers", _totalHandlers.ToString(), new Color(1f, 0.85f, 0.3f));

            foreach (var kv in _handlerCounts.OrderBy(kv => kv.Key))
                AddInfoRow(root, $"  {kv.Key}", kv.Value.ToString(), new Color(0.8f, 0.8f, 0.6f));

            // Entity State
            AddSectionHeader(root, "Entity State");
            AddEntityRow(root, "hero", _hero);
            AddEntityRow(root, "enemy_a", _enemyA);
            AddEntityRow(root, "enemy_b", _enemyB);
            AddEntityRow(root, "enemy_c", _enemyC);
            AddEntityRow(root, "enemy_d", _enemyD);
            AddEntityRow(root, "ally_a", _allyA);

            // Event Counts
            AddSectionHeader(root, "Event Counts");
            AddInfoRow(root, "Damage Events", _damageEvents.Count.ToString(), new Color(1f, 0.3f, 0.3f));
            AddInfoRow(root, "Heal Events", _healEvents.Count.ToString(), new Color(0.2f, 0.8f, 0.2f));
            AddInfoRow(root, "Status Applied", _statusApplied.Count.ToString(), new Color(0.8f, 0.6f, 0.2f));
            AddInfoRow(root, "Push Events", _pushEvents.Count.ToString(), new Color(0.5f, 0.7f, 1f));
        }

        private void AddEntityRow(VisualElement parent, string name, EntityId id)
        {
            int hp = _entityStats.GetCurrentHealth(id);
            int maxHp = _entityStats.GetStat(id, StatType.MaxHealth);
            var effects = _statusEffects.GetEffects(id);
            var effectStr = effects.Count > 0
                ? string.Join(", ", effects.Select(e => e.Type.ToString()))
                : "none";
            AddInfoRow(parent, name, $"HP={hp}/{maxHp} Effects=[{effectStr}]",
                hp < maxHp ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f));
        }

        private static void AddSectionHeader(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.style.fontSize = 18;
            label.style.color = Color.white;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 12;
            label.style.marginBottom = 6;
            parent.Add(label);
        }

        private static void AddInfoRow(VisualElement parent, string label, string value, Color valueColor)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;

            var nameLabel = new Label($"{label}: ");
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            nameLabel.style.width = 180;
            row.Add(nameLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 14;
            valueLabel.style.color = valueColor;
            row.Add(valueLabel);

            parent.Add(row);
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                // 1. Scene root
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),

                // 2. Execution service
                new("Execution service initialized", _executionService != null,
                    _executionService != null ? null : "Service is null"),

                // 3. All 13 words completed
                new("All 13 words completed", _wordsCompleted == 13,
                    _wordsCompleted == 13 ? null : $"Only {_wordsCompleted}/13 completed"),

                // 4-13. Each handler invoked
                new("Damage handler invoked", HasHandler("Damage"),
                    HasHandler("Damage") ? null : "Damage handler not invoked"),
                new("Heal handler invoked", HasHandler("Heal"),
                    HasHandler("Heal") ? null : "Heal handler not invoked"),
                new("Burn handler invoked", HasHandler("Burn"),
                    HasHandler("Burn") ? null : "Burn handler not invoked"),
                new("Water handler invoked", HasHandler("Water"),
                    HasHandler("Water") ? null : "Water handler not invoked"),
                new("Push handler invoked", HasHandler("Push"),
                    HasHandler("Push") ? null : "Push handler not invoked"),
                new("Shock handler invoked", HasHandler("Shock"),
                    HasHandler("Shock") ? null : "Shock handler not invoked"),
                new("Fear handler invoked", HasHandler("Fear"),
                    HasHandler("Fear") ? null : "Fear handler not invoked"),
                new("Stun handler invoked", HasHandler("Stun"),
                    HasHandler("Stun") ? null : "Stun handler not invoked"),
                new("Summon handler invoked", HasHandler("Summon"),
                    HasHandler("Summon") ? null : "Summon handler not invoked"),

                // 14. All 9 action handlers used
                new("All 9 action handlers used", _handlerCounts.Count >= 9,
                    _handlerCounts.Count >= 9 ? null : $"Only {_handlerCounts.Count}/9 handlers used"),

                // 15. Damage events
                new("Damage events fired", _damageEvents.Count > 0,
                    _damageEvents.Count > 0 ? null : "No damage events"),

                // 16. Heal events
                new("Heal events fired", _healEvents.Count > 0,
                    _healEvents.Count > 0 ? null : "No heal events"),

                // 17. Status effects applied
                new("Status effects applied", _statusApplied.Count > 0,
                    _statusApplied.Count > 0 ? null : "No status effects applied"),

                // 18. Push events
                new("Push events published", _pushEvents.Count > 0,
                    _pushEvents.Count > 0 ? null : "No push events"),

                // 20. Summon entity detected
                new("Summon entity detected", _summonDetected,
                    _summonDetected ? null : "No summoned entity registered"),

                // 21. Total handler executions >= 20
                new("Total handler executions >= 20", _totalHandlers >= 20,
                    _totalHandlers >= 20 ? null : $"Only {_totalHandlers} handler executions"),
            };

            return new ScenarioVerificationResult(checks);
        }

        private bool HasHandler(string actionId) =>
            _handlerCounts.ContainsKey(actionId) && _handlerCounts[actionId] > 0;

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();

            (_executionService as IDisposable)?.Dispose();
            (_statusEffects as IDisposable)?.Dispose();
            (_slotService as IDisposable)?.Dispose();
            (_entityStats as IDisposable)?.Dispose();
            (_turnService as IDisposable)?.Dispose();
            _executionService = null;
            _statusEffects = null;
            _slotService = null;
            _entityStats = null;
            _turnService = null;
            _eventBus = null;
        }
    }
}
