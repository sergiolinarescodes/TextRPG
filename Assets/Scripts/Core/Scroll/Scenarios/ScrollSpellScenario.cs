using System;
using System.Collections.Generic;
using System.Linq;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;
using Unidad.Core.UI.TextAnimation.ElementAnimation;
using Unidad.Core.UI.Tooltip;
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

        private RunSession _session;
        private ICombatLoopService _combatLoop;
        private ICombatAIService _combatAI;
        private ScenarioEncounterAdapter _encounterAdapter;

        // Controllers
        private WordInputController _wordInput;
        private EquipmentVisualController _equipment;
        private LootOverlayController _loot;
        private CombatVisualController _combat;
        private GameMessageController _messages;
        private PlayerStatsBarVisual _playerStatsBar;
        private CombatSlotVisual _slotVisual;

        // Cross-cutting
        private EntityTooltipService _tooltipService;
        private ITooltipService _frameworkTooltipService;
        private StatusEffectVisualService _statusVisualService;
        private Func<EntityId, Vector3> _positionProvider;

        private readonly List<IDisposable> _subscriptions = new();
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

            // --- Create run session (all run-lifetime services) ---
            _session = RunSessionFactory.Create(playerId, SceneRoot, new LiveAnimationResolver());
            var s = _session;

            // --- Learn a scroll immediately ---
            var rng = new System.Random();
            var excludes = s.SpellService.OfferedOriginals as HashSet<string>
                ?? new HashSet<string>(s.SpellService.OfferedOriginals);
            var scroll = ScrollGenerator.Generate(s.WordResolver, excludes, rng);
            if (scroll != null)
            {
                s.SpellService.LearnSpell(scroll);
                Debug.Log($"[ScrollSpell] Learned scroll: \"{scroll.ScrambledWord}\" (from \"{scroll.OriginalWord}\", cost={scroll.ManaCost})");
            }
            else
            {
                Debug.LogWarning("[ScrollSpell] No MagicDamage words found — no scroll generated");
            }

            // --- Build UI layout ---
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

            // Spell list label at top
            _spellListLabel = new Label("Spells: (none)");
            _spellListLabel.style.fontSize = 16;
            _spellListLabel.style.color = ScrollDefinition.ScrollPurple;
            _spellListLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _spellListLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _spellListLabel.style.backgroundColor = new Color(0.05f, 0.02f, 0.1f);
            _spellListLabel.style.paddingTop = 4;
            _spellListLabel.style.paddingBottom = 4;
            root.Insert(0, _spellListLabel);

            // Enemy row
            var enemyRow = new VisualElement();
            enemyRow.style.flexDirection = FlexDirection.Row;
            enemyRow.style.justifyContent = Justify.Center;
            enemyRow.style.alignItems = Align.Center;
            enemyRow.style.height = Length.Percent(20);
            enemyRow.style.backgroundColor = Color.black;
            root.Add(enemyRow);

            _slotVisual = new CombatSlotVisual(s.UnitService, s.EntityStats, s.SlotService);
            _slotVisual.BuildEnemyRow(enemyRow);

            // Middle area: left bar | text panel | right bar
            var middleArea = new VisualElement();
            middleArea.style.flexDirection = FlexDirection.Row;
            middleArea.style.flexGrow = 1;

            // Stats bar (created early so WordInputController can reference it for mana preview)
            _playerStatsBar = new PlayerStatsBarVisual(s.EventBus, s.EntityStats, s.StatusEffects,
                playerId, s.ResourceService);

            // Equipment controller (builds left + right bars)
            _equipment = new EquipmentVisualController(s.EventBus, s.EquipmentService,
                s.InventoryService, s.ItemRegistry, s.WeaponService, s.ConsumableService,
                s.PlayerInventoryId, playerId, root,
                () => _encounterAdapter?.IsEncounterActive == true);
            _equipment.BuildBars(middleArea);

            // Word input controller (builds main text panel)
            _wordInput = new WordInputController(s.EventBus, s.WordInputService, s.DrunkLetterService,
                s.WordMatchService, s.AmmoMatchService, s.WordResolver, s.CombatContext,
                s.PreviewService, s.AmmoPreviewService, s.WeaponService, s.ConsumableService,
                s.LetterReserve, _playerStatsBar, _slotVisual, playerId, fontScaleFactor);

            var mainTextPanel = _wordInput.BuildInputArea(vibrationAmplitude);
            middleArea.Insert(1, mainTextPanel);

            // Wire weapon/consumable click actions
            _equipment.FireWeaponAction = () => _wordInput.FireWeapon();
            _equipment.UseConsumableAction = () => _wordInput.UseConsumable();

            // Ally row
            var allyRow = new VisualElement();
            allyRow.pickingMode = PickingMode.Ignore;
            allyRow.style.position = Position.Absolute;
            allyRow.style.bottom = 10;
            allyRow.style.left = 0;
            allyRow.style.right = 0;
            allyRow.style.flexDirection = FlexDirection.Row;
            allyRow.style.justifyContent = Justify.Center;
            allyRow.style.alignItems = Align.Center;
            allyRow.style.height = 100;
            allyRow.style.paddingTop = 4;
            allyRow.style.paddingBottom = 4;
            mainTextPanel.Add(allyRow);
            _slotVisual.BuildAllyRow(allyRow);

            root.Add(middleArea);

            // Stats bar visual
            root.Add(_playerStatsBar.Build());

            // Projectile overlay
            var projectileOverlay = ProjectilePool.CreateOverlay();
            root.Add(projectileOverlay);

            // Position provider
            _positionProvider = entityId =>
            {
                if (entityId.Equals(playerId))
                {
                    var hpCenter = _playerStatsBar.HpBar.worldBound.center;
                    return new Vector3(hpCenter.x, hpCenter.y, 0f);
                }
                var element = _slotVisual.GetSlotElement(entityId);
                if (element != null)
                {
                    var center = element.worldBound.center;
                    return new Vector3(center.x, center.y, 0f);
                }
                return Vector3.zero;
            };
            s.AnimationService.Initialize(_positionProvider, projectileOverlay);

            // Status effect visual overlays
            var statusTextOverlay = StatusEffectFloatingTextPool.CreateOverlay();
            root.Add(statusTextOverlay);
            _statusVisualService = s.StatusVisualService;
            _statusVisualService.Initialize(_positionProvider, statusTextOverlay, _slotVisual);

            // Game message controller
            _messages = new GameMessageController(s.EventBus, _positionProvider, playerId);
            root.Add(_messages.CreateOverlay());

            // Combat visual controller
            _combat = new CombatVisualController(s.EventBus, s.EntityStats, s.UnitService,
                _slotVisual, s.AllUnits, playerId);

            // Tooltip layer
            var tooltipLayer = new VisualElement { name = "tooltip-layer" };
            tooltipLayer.style.position = Position.Absolute;
            tooltipLayer.style.left = 0;
            tooltipLayer.style.top = 0;
            tooltipLayer.style.right = 0;
            tooltipLayer.style.bottom = 0;
            tooltipLayer.pickingMode = PickingMode.Ignore;
            root.Add(tooltipLayer);

            // Framework tooltip service
            var elementAnimator = new ElementAnimator();
            _frameworkTooltipService = new TooltipService(s.EventBus, elementAnimator);
            _frameworkTooltipService.SetTooltipLayer(tooltipLayer);

            // Entity tooltip service (initialized after combat setup below)
            _tooltipService = new EntityTooltipService(s.EventBus);

            // Loot overlay controller
            _loot = new LootOverlayController(s.EventBus, s.LootRewardService,
                _equipment.RightBar, root, enabled => _wordInput.SetInputEnabled(enabled));

            // --- Start combat encounter (manual setup, not run-based) ---
            StartCombat(playerId, s);

            // Initialize tooltip service (needs encounterAdapter from combat setup)
            _tooltipService.Initialize(
                _slotVisual.GetAllSlotElements(),
                _equipment.RightBar.GetAllSlotElements(),
                _equipment.LeftBar.GetAllSlotElements(),
                _frameworkTooltipService,
                s.SlotService, s.StatusEffects, s.UnitService, s.PassiveService,
                _encounterAdapter, s.ActionRegistry, s.HandlerRegistry, s.EnemyResolver,
                s.AmmoResolver, s.WeaponService, s.ConsumableService, s.EquipmentService,
                s.ItemRegistry, s.EntityStats, s.InventoryService, s.PlayerInventoryId, playerId);

            // --- Event subscriptions ---
            _subscriptions.Add(s.EventBus.Subscribe<SpellLearnedEvent>(_ => RefreshSpellListLabel()));

            _subscriptions.Add(s.EventBus.Subscribe<GameOverEvent>(_ =>
            {
                _wordInput.SetInputEnabled(false);
                _playerStatsBar.HpLabel.text = "DEAD";
                _playerStatsBar.HpLabel.style.color = Color.red;
                Debug.Log("[ScrollSpell] GAME OVER");
            }));

            // Debug log subscriptions
            _subscriptions.Add(s.EventBus.Subscribe<WordSubmittedEvent>(evt =>
            {
                var word = evt.Word.ToLowerInvariant();
                var actions = s.WordResolver.Resolve(word);
                var meta = s.WordResolver.GetStats(word);
                if (actions.Count > 0)
                    Debug.Log($"[ScrollSpell] \"{evt.Word}\" -> {meta.Target} cost={meta.Cost} | {string.Join(", ", actions.Select(a => $"{a.ActionId}({a.Value})"))}");
                else
                    Debug.Log($"[ScrollSpell] \"{evt.Word}\" -> no actions");
            }));
            _subscriptions.Add(s.EventBus.Subscribe<PlayerTurnStartedEvent>(evt =>
                Debug.Log($"[Turn] Player turn #{evt.TurnNumber} (Round #{evt.RoundNumber})")));
            _subscriptions.Add(s.EventBus.Subscribe<ActionHandlerExecutedEvent>(evt =>
            {
                if (_combatLoop != null && !_combatLoop.IsPlayerTurn)
                    Debug.Log($"[Turn:Enemy] {evt.ActionId}({evt.Value}) -> {evt.Targets.Count} target(s)");
            }));

            // Wire combat loop to word input controller
            _wordInput.SetCombatLoop(_combatLoop);

            RefreshSpellListLabel();
            Debug.Log("[ScrollSpellScenario] Started — type the scrambled spell word to cast it!");
        }

        private void StartCombat(EntityId playerId, RunSession s)
        {
            (s.EnemyResolver as EnemyWordResolver)?.Clear();

            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(playerId);
            _encounterAdapter.SetEventBus(s.EventBus);

            var enemyDef = new EntityDefinition("Golem", 40, 4, 3, 3, 2, 0,
                new Color(0.5f, 0.5f, 0.6f),
                Array.Empty<string>(),
                Passives: Array.Empty<PassiveEntry>(),
                Tags: Array.Empty<string>());

            var entityId = new EntityId("golem_0");
            s.EntityStats.RegisterEntity(entityId, enemyDef.MaxHealth, enemyDef.Strength,
                enemyDef.MagicPower, enemyDef.PhysicalDefense, enemyDef.MagicDefense, enemyDef.Luck);
            s.UnitService.Register(new UnitId("golem_0"),
                new UnitDefinition(new UnitId("golem_0"), enemyDef.Name,
                    enemyDef.MaxHealth, enemyDef.Strength, enemyDef.PhysicalDefense, enemyDef.Luck, enemyDef.Color));
            s.SlotService.RegisterEnemy(entityId, 0);
            _encounterAdapter.RegisterEnemy(entityId, enemyDef);

            // Register enemy words
            var matchKey = s.AllUnits.FirstOrDefault(kvp => kvp.Value.Name == "Golem").Key;
            if (matchKey != null)
                UnitDatabaseLoader.RegisterUnitWords(s.EnemyResolver as EnemyWordResolver, matchKey);

            _encounterAdapter.Activate();
            ((CombatContext)s.CombatContext).SetEnemies(new[] { entityId });
            ((CombatContext)s.CombatContext).SetAllies(Array.Empty<EntityId>());

            // CombatAI
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(s.StatusEffects);
            _combatAI = new CombatAIService(s.EventBus, _encounterAdapter, s.EntityStats,
                s.TurnService, s.SlotService, s.CombatContext, s.ActionExecution, scorers,
                s.EnemyResolver as EnemyWordResolver, s.AllUnits, statusEffects: s.StatusEffects);

            // Turn order
            var turnOrder = new List<EntityId> { playerId, entityId };
            s.TurnService.SetTurnOrder(turnOrder);

            // CombatLoop
            _combatLoop = new CombatLoopService(
                s.EventBus, s.TurnService, s.EntityStats, s.WordResolver, s.WeaponService, playerId,
                s.ConsumableService, reservedWordHandler: null, combatContext: s.CombatContext,
                wordCooldown: s.WordCooldown);
            ((CombatLoopService)_combatLoop).Start();

            // Track enemy death
            _subscriptions.Add(s.EventBus.Subscribe<EntityDiedEvent>(evt =>
            {
                _encounterAdapter?.MarkDead(evt.EntityId);
            }));
        }

        private void RefreshSpellListLabel()
        {
            if (_spellListLabel == null || _session?.SpellService == null) return;
            var spells = _session.SpellService.LearnedSpells;
            _spellListLabel.text = spells.Count > 0
                ? $"Spells: {string.Join(", ", spells.Select(sp => sp.ToUpperInvariant()))}"
                : "Spells: (none)";
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var hasSpells = _session?.SpellService?.LearnedSpells.Count > 0;
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _wordInput?.CodeField != null,
                    _wordInput?.CodeField != null ? null : "Code field is null"),
                new("Spell service created", _session?.SpellService != null,
                    _session?.SpellService != null ? null : "SpellService is null"),
                new("At least one spell learned", hasSpells == true,
                    hasSpells == true ? null : "No spells were learned"),
                new("Combat loop active", _combatLoop != null,
                    _combatLoop != null ? null : "CombatLoop is null"),
                new("Spell resolver has words", _session?.SpellResolver?.WordCount > 0,
                    _session?.SpellResolver?.WordCount > 0 ? null : "EnemyWordResolver is empty"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();

            _tooltipService?.Dispose();
            _tooltipService = null;
            (_frameworkTooltipService as IDisposable)?.Dispose();
            _frameworkTooltipService = null;
            _positionProvider = null;

            _loot?.Dispose();
            _loot = null;
            _messages?.Dispose();
            _messages = null;
            _combat?.Dispose();
            _combat = null;
            _equipment?.Dispose();
            _equipment = null;
            _wordInput?.Dispose();
            _wordInput = null;
            _playerStatsBar?.Dispose();
            _playerStatsBar = null;

            (_combatLoop as IDisposable)?.Dispose();
            _combatLoop = null;
            (_combatAI as IDisposable)?.Dispose();
            _combatAI = null;
            _encounterAdapter?.EndEncounter();
            _encounterAdapter = null;

            _session?.Dispose();
            _session = null;

            _slotVisual = null;
            _spellListLabel = null;
        }

        private sealed class LiveAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
