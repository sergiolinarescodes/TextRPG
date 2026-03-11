using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Testing;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Scroll.Scenarios
{
    internal sealed class ScrollSpellScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter VibrationAmplitudeParam = new(
            "vibrationAmplitude", "Vibration Amplitude", typeof(float), 3.0f, 0f, 10f);

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private LiveScenarioServices _svc;
        private LiveScenarioLayout _layout;
        private IWordCooldownService _wordCooldown;
        private SpellWordResolver _spellResolver;
        private ISpellService _spellService;
        private ICombatLoopService _combatLoop;
        private ICombatAIService _combatAI;
        private ScenarioEncounterAdapter _encounterAdapter;
        private CompositeWordResolver _compositeResolver;
        private readonly List<IDisposable> _subscriptions = new();
        private Dictionary<string, EntityDefinition> _allUnits;
        private Label _spellListLabel;

        public ScrollSpellScenario() : base(new TestScenarioDefinition(
            "scroll-spell",
            "Scroll / Spell Learning (Live)",
            "Learn spell scrolls and use them in combat.\n" +
            "1. A scroll is auto-learned at start (scrambled MagicDamage word, -1 mana, 2-round fixed cooldown)\n" +
            "2. Enter combat against a test enemy\n" +
            "3. Type the scrambled spell word to cast it\n" +
            "4. Observe: reduced mana cost, flat 2-round cooldown (no escalation), normal words still escalate",
            new[] { VibrationAmplitudeParam, FontScaleFactorParam }
        )) { }

        protected override void ExecuteInternal(ScenarioParameterOverrides overrides)
        {
            var vibrationAmplitude = ResolveParam<float>(overrides, "vibrationAmplitude");
            var fontScaleFactor = ResolveParam<float>(overrides, "fontScaleFactor");

            var playerId = new EntityId("player");

            // Core services
            _svc = LiveScenarioHelper.CreateCoreServices(playerId, SceneRoot);

            // Word cooldown
            _wordCooldown = new WordCooldownService();

            // Spell system — run-lifetime
            _spellResolver = new SpellWordResolver();
            _spellService = new SpellService(_svc.EventBus, _svc.WordResolver, _wordCooldown, _spellResolver,
                _svc.WordActionData.TagResolver);

            // Composite resolver: spell resolver first (priority), then player, then enemy
            var enemyResolver = new EnemyWordResolver();
            _svc.EnemyResolver = enemyResolver;
            _compositeResolver = new CompositeWordResolver(_spellResolver, _svc.WordResolver, enemyResolver);

            // Rebuild match service with composite resolver so spell words are detected while typing
            _svc.WordMatchService = new WordMatchService(_compositeResolver, _svc.WordActionData.ActionRegistry);

            // Encounter adapter
            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(playerId);
            _encounterAdapter.SetEventBus(_svc.EventBus);
            _svc.EncounterAdapter = _encounterAdapter;

            // Action execution
            LiveScenarioHelper.CreateActionExecution(_svc, _compositeResolver);

            // Passive system
            _allUnits = UnitDatabaseLoader.LoadAll();
            LiveScenarioHelper.CreatePassiveService(_svc, _encounterAdapter, _allUnits);

            // Equipment & loot
            LiveScenarioHelper.CreateEquipmentAndLoot(_svc);

            // Build layout
            _layout = LiveScenarioHelper.BuildLayout(RootVisualElement, _svc, vibrationAmplitude, fontScaleFactor);

            // Spell list label (shows learned spells at top)
            _spellListLabel = new Label("Spells: (none)");
            _spellListLabel.style.fontSize = 16;
            _spellListLabel.style.color = ScrollDefinition.ScrollPurple;
            _spellListLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _spellListLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _spellListLabel.style.backgroundColor = new Color(0.05f, 0.02f, 0.1f);
            _spellListLabel.style.paddingTop = 4;
            _spellListLabel.style.paddingBottom = 4;
            RootVisualElement.Insert(0, _spellListLabel);

            // Learn a scroll immediately
            var rng = new System.Random();
            var scroll = ScrollGenerator.Generate(_svc.WordResolver, ((SpellService)_spellService).OfferedOriginals, rng);
            if (scroll != null)
            {
                _spellService.LearnSpell(scroll);
                Debug.Log($"[ScrollSpell] Learned scroll: \"{scroll.ScrambledWord}\" (from \"{scroll.OriginalWord}\", cost={scroll.ManaCost})");
            }
            else
            {
                Debug.LogWarning("[ScrollSpell] No MagicDamage words found — no scroll generated");
            }

            // Start combat encounter
            StartCombat(playerId, enemyResolver);

            // Common event subscriptions
            LiveScenarioHelper.SubscribeCommonEvents(_svc, _layout, _subscriptions,
                () => _combatLoop?.IsPlayerTurn == true, _allUnits);

            // Spell learned event
            _subscriptions.Add(_svc.EventBus.Subscribe<SpellLearnedEvent>(evt =>
            {
                Debug.Log($"[ScrollSpell] Spell learned: {evt.ScrambledWord} (from {evt.OriginalWord}, cost={evt.ManaCost})");
                var pos = _svc.PositionProvider?.Invoke(playerId) ?? Vector3.zero;
                _svc.GameMessages?.Spawn(new Vector2(pos.x, pos.y),
                    $"Learned: {evt.ScrambledWord.ToUpperInvariant()}", ScrollDefinition.ScrollPurple);
                RefreshSpellListLabel();
            }));

            // Cooldown event
            _subscriptions.Add(_svc.EventBus.Subscribe<WordCooldownEvent>(evt =>
            {
                var msg = evt.Permanent
                    ? $"\"{evt.Word}\" exhausted!"
                    : $"\"{evt.Word}\" cooldown ({evt.RemainingRounds}r)";
                Debug.Log($"[WordCooldown] {msg}");
                var pos = _svc.PositionProvider?.Invoke(playerId) ?? Vector3.zero;
                _svc.GameMessages?.Spawn(new Vector2(pos.x, pos.y), msg, new Color(1f, 0.4f, 0.4f));
            }));

            // Player turn
            _subscriptions.Add(_svc.EventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
            {
                LiveScenarioHelper.SetInputEnabled(_layout, true);
                Debug.Log($"[Turn] Player turn #{evt.TurnNumber} (Round #{evt.RoundNumber})");
            }));
            _subscriptions.Add(_svc.EventBus.Subscribe<PlayerTurnEndedEvent>(_ =>
                LiveScenarioHelper.SetInputEnabled(_layout, false)));
            _subscriptions.Add(_svc.EventBus.Subscribe<GameOverEvent>(_ =>
            {
                LiveScenarioHelper.SetInputEnabled(_layout, false);
                Debug.Log("[ScrollSpell] GAME OVER");
            }));

            // Input handling
            LiveScenarioHelper.SetupInputHandling(_layout, _svc,
                submitFunc: word => _combatLoop?.SubmitWord(word) ?? WordSubmitResult.InvalidWord,
                canFireWeapon: () => _combatLoop?.FireWeapon() == true,
                canUseConsumable: () => _combatLoop?.UseConsumable() == true,
                isEncounterActive: () => _encounterAdapter?.IsEncounterActive == true,
                fontScaleFactor: fontScaleFactor,
                subs: _subscriptions);

            RefreshSpellListLabel();
            Debug.Log("[ScrollSpellScenario] Started — type the scrambled spell word to cast it!");
        }

        private void StartCombat(EntityId playerId, EnemyWordResolver enemyResolver)
        {
            enemyResolver.Clear();

            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(playerId);
            _encounterAdapter.SetEventBus(_svc.EventBus);
            _svc.EncounterAdapter = _encounterAdapter;

            ((CombatSlotService)_svc.SlotService).Initialize();

            // Spawn a single test enemy
            var enemyDef = new EntityDefinition(
                "Golem", 40, 4, 3, 3, 2, 0,
                new Color(0.5f, 0.5f, 0.6f),
                Array.Empty<string>(),
                Passives: Array.Empty<PassiveEntry>(),
                Tags: Array.Empty<string>());

            var entityId = new EntityId("golem_0");
            _svc.EntityStats.RegisterEntity(entityId, enemyDef.MaxHealth, enemyDef.Strength,
                enemyDef.MagicPower, enemyDef.PhysicalDefense, enemyDef.MagicDefense, enemyDef.Luck);
            _svc.UnitService.Register(new UnitId("golem_0"),
                new UnitDefinition(new UnitId("golem_0"), enemyDef.Name,
                    enemyDef.MaxHealth, enemyDef.Strength, enemyDef.PhysicalDefense, enemyDef.Luck, enemyDef.Color));
            _svc.SlotService.RegisterEnemy(entityId, 0);
            _encounterAdapter.RegisterEnemy(entityId, enemyDef);

            // Register enemy words
            var matchKey = _allUnits.FirstOrDefault(kvp => kvp.Value.Name == "Golem").Key;
            if (matchKey != null)
                UnitDatabaseLoader.RegisterUnitWords(enemyResolver, matchKey);

            _encounterAdapter.Activate();
            _svc.CombatContext.SetSourceEntity(playerId);
            _svc.CombatContext.SetEnemies(new[] { entityId });
            _svc.CombatContext.SetAllies(Array.Empty<EntityId>());

            // CombatAI
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(_svc.StatusEffects);
            _combatAI = new CombatAIService(_svc.EventBus, _encounterAdapter, _svc.EntityStats,
                _svc.TurnService, _svc.SlotService, _svc.CombatContext, _svc.ActionExecution, scorers,
                enemyResolver, _allUnits, statusEffects: _svc.StatusEffects);

            // Turn order
            var turnOrder = new List<EntityId> { playerId, entityId };
            _svc.TurnService.SetTurnOrder(turnOrder);

            // CombatLoop
            _combatLoop = new CombatLoopService(
                _svc.EventBus, _svc.TurnService, _svc.EntityStats, _compositeResolver, _svc.WeaponService,
                playerId, _svc.ConsumableService, reservedWordHandler: null,
                combatContext: _svc.CombatContext, wordCooldown: _wordCooldown);
            _combatLoop.Start();
        }

        private void RefreshSpellListLabel()
        {
            if (_spellListLabel == null || _spellService == null) return;
            var spells = _spellService.LearnedSpells;
            _spellListLabel.text = spells.Count > 0
                ? $"Spells: {string.Join(", ", spells.Select(s => s.ToUpperInvariant()))}"
                : "Spells: (none)";
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var hasSpells = _spellService?.LearnedSpells.Count > 0;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _layout?.CodeField != null,
                    _layout?.CodeField != null ? null : "Code field is null"),
                new("Spell service created", _spellService != null,
                    _spellService != null ? null : "SpellService is null"),
                new("At least one spell learned", hasSpells == true,
                    hasSpells == true ? null : "No spells were learned"),
                new("Combat loop active", _combatLoop != null,
                    _combatLoop != null ? null : "CombatLoop is null"),
                new("Spell resolver has words", _spellResolver?.WordCount > 0,
                    _spellResolver?.WordCount > 0 ? null : "SpellWordResolver is empty"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            (_combatLoop as IDisposable)?.Dispose();
            (_combatAI as IDisposable)?.Dispose();
            (_spellService as IDisposable)?.Dispose();
            LiveScenarioHelper.CleanupServices(_svc, _layout, _subscriptions);

            _combatLoop = null;
            _combatAI = null;
            _spellService = null;
            _spellResolver = null;
            _wordCooldown = null;
            _compositeResolver = null;
            _encounterAdapter = null;
            _allUnits = null;
            _spellListLabel = null;
            _svc = null;
            _layout = null;
        }
    }
}
