using System;
using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatAI;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.Services;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using Unidad.Core.Testing;
using Unidad.Core.UI.TextAnimation.ElementAnimation;
using Unidad.Core.UI.Tooltip;
using UnityEngine;
using UnityEngine.UIElements;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.WordCooldown.Scenarios
{
    internal sealed class GiveCooldownLiveScenario : DataDrivenScenario
    {
        private static readonly ScenarioParameter VibrationAmplitudeParam = new(
            "vibrationAmplitude", "Vibration Amplitude", typeof(float), 3.0f, 0f, 10f);

        private static readonly ScenarioParameter FontScaleFactorParam = new(
            "fontScaleFactor", "Font Scale Factor", typeof(float), 1.0f, 0.5f, 1f);

        private RunSession _session;
        private IEventEncounterService _encounterService;
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
        private Func<EntityId, Vector3> _positionProvider;
        private readonly List<IDisposable> _subscriptions = new();

        public GiveCooldownLiveScenario() : base(new TestScenarioDefinition(
            "give-cooldown-live",
            "Give Prefix + Cooldown + Mercenary (Live)",
            "Live interactive event encounter testing all three features:\n" +
            "- Type 'give flame' or 'give ember' to burn yourself (target inversion)\n" +
            "- Type 'give money' or 'give gold' to pay from gold resource (Pay action value)\n" +
            "- Type 'give silver' to consume a SILVER-tagged item from inventory\n" +
            "- Words go on escalating cooldown after use (2 -> 5 -> 10 -> 20 -> permanent)\n" +
            "- Rejection animation + floating message on cooldown\n" +
            "- Gold display shows current gold (starts at 50)",
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

            // Add gold resource (RunSession already defines Gold, just add amount)
            s.ResourceService.Add(ResourceIds.Gold, 50);

            // Silver bar items for "give silver" testing (consumed from inventory)
            var silverDef = new EquipmentItemDefinition("silver_bar", "SILVER BAR", EquipmentSlotType.Accessory,
                0, default, new Color(0.75f, 0.75f, 0.8f), Array.Empty<string>(),
                Array.Empty<PassiveEntry>(), new[] { "SILVER" });
            s.ItemRegistry.Register("silver_bar", silverDef);
            s.InventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(
                new ItemId("silver_bar"), "SILVER BAR", 99));
            s.InventoryService.Add(s.PlayerInventoryId, new ItemId("silver_bar"), 3);

            // Event encounter services
            _encounterService = new EventEncounterService(
                s.EventBus, s.EntityStats, s.SlotService, s.CombatContext, s.ReactionService);
            s.ReactionContext.EncounterService = _encounterService;

            // Encounter adapter (no auto-end — lifecycle managed by EventEncounterService)
            _encounterAdapter = new ScenarioEncounterAdapter();
            _encounterAdapter.SetPlayer(playerId);
            _encounterAdapter.SetEventBus(s.EventBus);
            _encounterAdapter.SetAutoEndOnAllDead(false);

            // Build encounter: mercenary + flammable barrel
            var encounterDef = BuildTestEncounter();

            // Register interactable UnitDefinitions
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");
                var uid = new UnitId(entityId.Value);
                s.UnitService.Register(uid,
                    new UnitDefinition(uid, def.Name, def.MaxHealth, 0, 0, 0, def.Color));
            }

            // Start encounter
            _encounterService.StartEncounter(encounterDef, playerId);

            // Register interactables as enemies in adapter
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                var entityId = _encounterService.InteractableEntities[i];
                var entityDef = new EntityDefinition(
                    def.Name, def.MaxHealth, 0, 0, 0, 0, 0, def.Color,
                    Array.Empty<string>(), 0, "interactable", def.Passives,
                    def.Tags, def.Description, def.DeathReward, def.DeathRewardValue);
                _encounterAdapter.RegisterEnemy(entityId, entityDef);
            }
            _encounterAdapter.Activate();

            // Register interactable passives
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var entityId = _encounterService.InteractableEntities[i];
                    s.PassiveService.RegisterPassives(entityId, def.Passives);
                }
            }

            // CombatAI (interactables auto-pass since empty abilities)
            var scorers = CombatAISystemInstaller.CreateScorerRegistry(s.StatusEffects);
            _combatAI = new CombatAIService(s.EventBus, _encounterAdapter, s.EntityStats,
                s.TurnService, s.SlotService, s.CombatContext, s.ActionExecution, scorers,
                s.EnemyResolver as EnemyWordResolver, s.AllUnits, statusEffects: s.StatusEffects);

            // Turn order: player first, then interactables
            var turnOrder = new List<EntityId> { playerId };
            turnOrder.AddRange(_encounterService.InteractableEntities);
            s.TurnService.SetTurnOrder(turnOrder);

            // CombatLoopService (unified loop) with cooldown + combat context for "give" prefix
            _combatLoop = new CombatLoopService(
                s.EventBus, s.TurnService, s.EntityStats, s.WordResolver, s.WeaponService, playerId,
                s.ConsumableService, combatContext: s.CombatContext, wordCooldown: s.WordCooldown,
                giveValidator: s.GiveValidator, statusEffects: s.StatusEffects);
            ((CombatLoopService)_combatLoop).Start();

            // No max turns for live scenario

            // Mark dead entities in adapter
            _subscriptions.Add(s.EventBus.Subscribe<EntityDiedEvent>(evt =>
                _encounterAdapter.MarkDead(evt.EntityId)));

            // --- Build UI layout ---
            var root = RootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);

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
            _playerStatsBar = new PlayerStatsBarVisual(s.EventBus, s.EntityStats, s.StatusEffects, playerId, s.ResourceService);

            // Equipment controller (builds left + right bars)
            _equipment = new EquipmentVisualController(s.EventBus, s.EquipmentService,
                s.InventoryService, s.ItemRegistry, s.WeaponService, s.ConsumableService,
                s.PlayerInventoryId, playerId, root);
            _equipment.BuildBars(middleArea);

            // Word input controller (builds main text panel)
            _wordInput = new WordInputController(s.EventBus, s.WordInputService, s.DrunkLetterService,
                s.WordMatchService, s.AmmoMatchService, s.WordResolver, s.CombatContext,
                s.PreviewService, s.AmmoPreviewService, s.WeaponService, s.ConsumableService,
                s.LetterReserve, _playerStatsBar, _slotVisual, playerId, fontScaleFactor);

            var mainTextPanel = _wordInput.BuildInputArea(vibrationAmplitude);
            middleArea.Insert(1, mainTextPanel);

            // Wire weapon/consumable click actions (not used in event encounter but keeps controller consistent)
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
            s.StatusVisualService.Initialize(_positionProvider, statusTextOverlay, _slotVisual);

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

            // Entity tooltip service
            _tooltipService = new EntityTooltipService(s.EventBus);
            _tooltipService.Initialize(
                _slotVisual.GetAllSlotElements(),
                _equipment.RightBar.GetAllSlotElements(),
                _equipment.LeftBar.GetAllSlotElements(),
                _frameworkTooltipService,
                s.SlotService, s.StatusEffects, s.UnitService, s.PassiveService,
                null, s.ActionRegistry, s.HandlerRegistry, s.EnemyResolver,
                s.AmmoResolver, s.WeaponService, s.ConsumableService, s.EquipmentService,
                s.ItemRegistry, s.EntityStats, s.InventoryService, s.PlayerInventoryId, playerId);
            _tooltipService.SetEventEncounterService(_encounterService);

            // Loot overlay controller
            _loot = new LootOverlayController(s.EventBus, s.LootRewardService,
                _equipment.RightBar, root, enabled => _wordInput.SetInputEnabled(enabled));

            // Wire input to unified combat loop
            _wordInput.SetCombatLoop(_combatLoop);
            _wordInput.SetInputEnabled(true);

            // Recruitment event
            _subscriptions.Add(s.EventBus.Subscribe<EntityRecruitedEvent>(evt =>
                Debug.Log($"[Recruitment] {evt.EntityId.Value} recruited by {evt.Recruiter.Value}!")));

            // Encounter lifecycle
            _subscriptions.Add(s.EventBus.Subscribe<EventEncounterEndedEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Encounter ended: {evt.EncounterId}");
                _wordInput.SetInputEnabled(false);
            }));

            Debug.Log("[GiveCooldownLiveScenario] Started — try: 'give fire', 'give money' (gold), 'give silver' (item), repeat words for cooldown");
        }

        private static EventEncounterDefinition BuildTestEncounter()
        {
            var mercenary = new InteractableDefinition(
                "Sellsword",
                50,
                new Color(0.9f, 0.75f, 0.3f),
                new[]
                {
                    new InteractionReaction("Trade", "message", null, 0),
                    new InteractionReaction("Recruit", "message", "Not for free.", 0),
                },
                Description: "A sword-for-hire. Pay enough gold and they'll join you.",
                Tags: new[] { "mercenary" });

            var barrel = new InteractableDefinition(
                "Barrel",
                15,
                new Color(0.6f, 0.4f, 0.2f),
                new[]
                {
                    new InteractionReaction("Search", "message", "Just an old barrel.", 0),
                    new InteractionReaction("Open", "reward", "random", 1),
                },
                Description: "A wooden barrel. Flammable.",
                Tags: new[] { "flammable", "breakable" });

            return new EventEncounterDefinition(
                "test_give_cooldown",
                "Mercenary Camp",
                new[] { mercenary, barrel });
        }

        protected override ScenarioVerificationResult VerifyInternal(ScenarioParameterOverrides overrides)
        {
            var checks = new List<ScenarioVerificationResult.CheckResult>
            {
                new("Scene root created", SceneRoot != null,
                    SceneRoot != null ? null : "No scene root"),
                new("AnimatedCodeField exists", _wordInput?.CodeField != null,
                    _wordInput?.CodeField != null ? null : "Code field is null"),
                new("Event encounter active", _encounterService?.IsEncounterActive == true,
                    _encounterService?.IsEncounterActive == true ? null : "Encounter not active"),
                new("Combat loop active", _combatLoop != null && !_combatLoop.IsGameOver,
                    _combatLoop != null && !_combatLoop.IsGameOver ? null : "Combat loop not active"),
                new("Word cooldown service created", _session?.WordCooldown != null,
                    _session?.WordCooldown != null ? null : "WordCooldownService is null"),
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
            (_encounterService as IDisposable)?.Dispose();
            _encounterService = null;
            _encounterAdapter?.EndEncounter();
            _encounterAdapter = null;

            _session?.Dispose();
            _session = null;

            _slotVisual = null;
        }

        private sealed class LiveAnimationResolver : IAnimationResolver
        {
            public bool IsInstant => false;
            public void Play(string animationId, Action onComplete = null) => onComplete?.Invoke();
        }
    }
}
