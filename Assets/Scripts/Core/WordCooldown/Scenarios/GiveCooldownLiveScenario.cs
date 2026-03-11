using System;
using System.Collections.Generic;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatLoop;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounterLoop;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Inventory;
using Unidad.Core.Testing;
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

        private LiveScenarioServices _svc;
        private LiveScenarioLayout _layout;
        private IEventEncounterService _encounterService;
        private IEventEncounterLoopService _loopService;
        private IWordCooldownService _wordCooldown;
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

            // Core services
            _svc = LiveScenarioHelper.CreateCoreServices(playerId, SceneRoot);

            // Word cooldown service
            _wordCooldown = new WordCooldownService();

            // Encounter adapter for passives
            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(playerId);
            encounterAdapter.SetEventBus(_svc.EventBus);
            encounterAdapter.Activate();
            _svc.EncounterAdapter = encounterAdapter;
            _svc.EnemyResolver = new EnemyWordResolver();

            // Action execution
            LiveScenarioHelper.CreateActionExecution(_svc, _svc.WordResolver);

            // Passive system
            var allUnits = UnitDatabaseLoader.LoadAll();
            LiveScenarioHelper.CreatePassiveService(_svc, encounterAdapter, allUnits);

            // Equipment & loot (must come before encounter context so inventory is available)
            LiveScenarioHelper.CreateEquipmentAndLoot(_svc);

            // Event encounter services with tag reactions (includes mercenary)
            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry();
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var encounterContext = new EventEncounterContext(
                _svc.EntityStats, _svc.SlotService, _svc.EventBus, _svc.StatusEffects,
                inventoryService: _svc.InventoryService, playerInventoryId: _svc.PlayerInventoryId,
                itemRegistry: _svc.ItemRegistry);
            var reactionService = new ReactionService(_svc.EventBus, outcomeRegistry, encounterContext, tagReactions, _svc.CombatContext);
            _encounterService = new EventEncounterService(
                _svc.EventBus, _svc.EntityStats, _svc.SlotService, _svc.CombatContext, reactionService);
            encounterContext.EncounterService = _encounterService;
            _svc.EventEncounterService = _encounterService;

            // Build encounter: mercenary + flammable barrel
            var encounterDef = BuildTestEncounter();

            // Register interactable UnitDefinitions
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                var entityId = new EntityId($"interactable_{def.Name.ToLowerInvariant()}_{i}");
                var uid = new UnitId(entityId.Value);
                _svc.UnitService.Register(uid,
                    new UnitDefinition(uid, def.Name, def.MaxHealth, 0, 0, 0, def.Color));
            }

            // Start encounter
            _encounterService.StartEncounter(encounterDef, playerId);

            // Register interactable passives
            for (int i = 0; i < encounterDef.Interactables.Length; i++)
            {
                var def = encounterDef.Interactables[i];
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var entityId = _encounterService.InteractableEntities[i];
                    _svc.PassiveService.RegisterPassives(entityId, def.Passives);
                }
            }

            // Gold resource for "give money"/"give gold" testing
            var resourceService = new Unidad.Core.Resource.ResourceService(_svc.EventBus);
            resourceService.Define(
                EventEncounter.Reactions.Tags.ResourceIds.Gold,
                new Unidad.Core.Resource.ResourceDefinition(0, 0, 99999));
            resourceService.Add(EventEncounter.Reactions.Tags.ResourceIds.Gold, 50);
            _svc.ResourceService = resourceService;

            // Silver bar items for "give silver" testing (consumed from inventory)
            var silverDef = new EquipmentItemDefinition("silver_bar", "SILVER BAR", EquipmentSlotType.Accessory,
                0, default, new Color(0.75f, 0.75f, 0.8f), System.Array.Empty<string>(),
                System.Array.Empty<Encounter.PassiveEntry>(), new[] { "SILVER" });
            _svc.ItemRegistry.Register("silver_bar", silverDef);
            _svc.InventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(
                new ItemId("silver_bar"), "SILVER BAR", 99));
            _svc.InventoryService.Add(_svc.PlayerInventoryId, new ItemId("silver_bar"), 3);

            // Give validator — Pay words deduct gold resource, SILVER-tagged words consume inventory items
            var giveValidator = new GiveValidator(
                _svc.WordActionData.TagResolver, _svc.InventoryService, _svc.PlayerInventoryId, _svc.ItemRegistry,
                _svc.WordResolver, resourceService);

            // Event encounter loop with cooldown + combat context for "give" prefix
            var loopService = new EventEncounterLoopService(
                _svc.EventBus, _svc.EntityStats, _svc.WordResolver, _encounterService, playerId,
                reservedWordHandler: null, combatContext: _svc.CombatContext, wordCooldown: _wordCooldown,
                giveValidator: giveValidator);
            _loopService = loopService;
            _loopService.Start();

            // Build UI
            _layout = LiveScenarioHelper.BuildLayout(RootVisualElement, _svc, vibrationAmplitude, fontScaleFactor);

            // Common event subscriptions
            LiveScenarioHelper.SubscribeCommonEvents(_svc, _layout, _subscriptions, () => true, allUnits);

            // Interaction message popup
            _subscriptions.Add(_svc.EventBus.Subscribe<InteractionMessageEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Message: {evt.Message}");
                var pos = _svc.PositionProvider?.Invoke(evt.SourceEntityId) ?? Vector3.zero;
                _svc.GameMessages?.Spawn(new Vector2(pos.x, pos.y), evt.Message, new Color(1f, 0.9f, 0.5f));
            }));

            // Word cooldown popup
            _subscriptions.Add(_svc.EventBus.Subscribe<WordCooldownEvent>(evt =>
            {
                var msg = evt.Permanent
                    ? $"\"{evt.Word}\" is permanently exhausted!"
                    : $"\"{evt.Word}\" on cooldown ({evt.RemainingRounds} rounds)";
                Debug.Log($"[WordCooldown] {msg}");
                var pos = _svc.PositionProvider?.Invoke(playerId) ?? Vector3.zero;
                _svc.GameMessages?.Spawn(new Vector2(pos.x, pos.y), msg, new Color(1f, 0.4f, 0.4f));
            }));

            // Recruitment event
            _subscriptions.Add(_svc.EventBus.Subscribe<EntityRecruitedEvent>(evt =>
                Debug.Log($"[Recruitment] {evt.EntityId.Value} recruited by {evt.Recruiter.Value}!")));

            // Encounter lifecycle
            _subscriptions.Add(_svc.EventBus.Subscribe<EventEncounterEndedEvent>(evt =>
            {
                Debug.Log($"[EventEncounter] Encounter ended: {evt.EncounterId}");
                LiveScenarioHelper.SetInputEnabled(_layout, false);
            }));

            // Input handling
            LiveScenarioHelper.SetupInputHandling(_layout, _svc,
                submitFunc: word => _loopService.SubmitWord(word),
                canFireWeapon: () => false,
                canUseConsumable: () => false,
                isEncounterActive: () => _encounterService.IsEncounterActive,
                fontScaleFactor: fontScaleFactor,
                subs: _subscriptions);

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
                new("AnimatedCodeField exists", _layout?.CodeField != null,
                    _layout?.CodeField != null ? null : "Code field is null"),
                new("Event encounter active", _encounterService?.IsEncounterActive == true,
                    _encounterService?.IsEncounterActive == true ? null : "Encounter not active"),
                new("Loop service active", _loopService?.IsActive == true,
                    _loopService?.IsActive == true ? null : "Loop not active"),
                new("Word cooldown service created", _wordCooldown != null,
                    _wordCooldown != null ? null : "WordCooldownService is null"),
            };
            return new ScenarioVerificationResult(checks);
        }

        protected override void OnCleanup()
        {
            (_loopService as IDisposable)?.Dispose();
            (_encounterService as IDisposable)?.Dispose();
            LiveScenarioHelper.CleanupServices(_svc, _layout, _subscriptions);

            _loopService = null;
            _encounterService = null;
            _wordCooldown = null;
            _svc = null;
            _layout = null;
        }
    }
}
