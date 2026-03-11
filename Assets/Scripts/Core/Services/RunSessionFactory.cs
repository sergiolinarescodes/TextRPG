using System.Collections.Generic;
using TextRPG.Core.ActionAnimation;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Consumable;
using TextRPG.Core.Encounter;
using TextRPG.Core.Equipment;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.Passive;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Run;
using TextRPG.Core.Scroll;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.UnitRendering;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using TextRPG.Core.WordInput;
using TextRPG.Core.WordInput.Scenarios;
using Unidad.Core.Abstractions;
using Unidad.Core.EventBus;
using Unidad.Core.Inventory;
using UnityEngine;
using EntityId = TextRPG.Core.EntityStats.EntityId;

namespace TextRPG.Core.Services
{
    internal static class RunSessionFactory
    {
        public static RunSession Create(EntityId playerId, GameObject sceneRoot, IAnimationResolver animResolver)
        {
            var eventBus = new EventBus();
            var drunkLetterService = new DrunkLetterService(eventBus, playerId);
            var wordInputService = new WordInputService(eventBus, drunkLetterService);
            var unitService = new UnitService(eventBus);
            var entityStats = new EntityStatsService(eventBus);

            var wordActionData = WordActionDatabaseLoader.Load();
            var wordResolver = new FilteredWordResolver(wordActionData.Resolver, wordActionData.AmmoWordSet);
            var ammoResolver = wordActionData.AmmoResolver;
            var actionRegistry = wordActionData.ActionRegistry;
            var wordTagResolver = wordActionData.TagResolver;
            var ammoMatchService = new WordMatchService(ammoResolver, actionRegistry);

            var slotService = new CombatSlotService(eventBus);
            slotService.Initialize();

            var combatContext = new CombatContext();
            combatContext.SetEntityStats(entityStats);
            combatContext.SetSlotService(slotService);

            var turnService = new TurnService(eventBus);

            // StatusEffect (circular ref: handlerContext <-> statusEffects)
            var effectHandlerRegistry = StatusEffectSystemInstaller.CreateHandlerRegistry();
            var handlerContext = new StatusEffectHandlerContext(entityStats, turnService, eventBus);
            var statusEffects = new StatusEffectService(eventBus, entityStats, turnService,
                effectHandlerRegistry, handlerContext);
            ((StatusEffectHandlerContext)handlerContext).StatusEffects = (IStatusEffectService)statusEffects;

            // Weapon + Item
            var weaponRegistry = WeaponSystemInstaller.BuildWeaponRegistry(wordActionData);
            var weaponService = new WeaponService(eventBus, weaponRegistry);
            var itemRegistry = EquipmentSystemInstaller.BuildItemRegistry(wordActionData);

            // Inventory
            var inventoryService = new InventoryService(eventBus);
            var playerInventoryId = new InventoryId("player");

            // Resource service
            var resourceService = new Unidad.Core.Resource.ResourceService(eventBus);
            resourceService.Define(
                EventEncounter.Reactions.Tags.ResourceIds.Gold,
                new Unidad.Core.Resource.ResourceDefinition(0, 0, 99999));

            // Reaction service (created early so action handlers can access entity tags)
            var tagReactions = EventEncounterSystemInstaller.CreateTagReactionRegistry();
            var outcomeRegistry = EventEncounterSystemInstaller.CreateOutcomeRegistry(null);
            var reactionContext = new EventEncounterContext(entityStats, slotService, eventBus, statusEffects,
                resourceService, inventoryService, playerInventoryId, itemRegistry);
            var reactionService = new ReactionService(eventBus, outcomeRegistry, reactionContext,
                tagReactions, combatContext);

            // Action handlers
            var actionHandlerCtx = new ActionHandlerContext(entityStats, eventBus, combatContext,
                statusEffects, turnService, weaponService, slotService: slotService,
                entityTagProvider: reactionService);
            var handlerRegistry = ActionHandlerFactory.CreateDefault(actionHandlerCtx);

            // Player entity
            PlayerDefaults.Register(entityStats, playerId);
            combatContext.SetSourceEntity(playerId);

            // Player inventory
            inventoryService.Create(playerInventoryId, new InventoryDefinition(EquipmentConstants.InventorySlotCount));
            foreach (var itemWord in itemRegistry.Keys)
            {
                if (itemRegistry.TryGet(itemWord, out var itemDef))
                    inventoryService.DefineItem(new Unidad.Core.Inventory.ItemDefinition(
                        new ItemId(itemWord), itemDef.DisplayName, 1));
            }

            // Static data
            var allUnits = UnitDatabaseLoader.LoadAll();
            var allEventEncounters = LoadEventEncounters();

            // Enemy + spell resolvers
            var enemyResolver = new EnemyWordResolver();
            var spellResolver = new EnemyWordResolver();

            // Composite resolver (spell first for priority)
            var compositeResolver = new CompositeWordResolver(spellResolver, wordResolver, enemyResolver);

            // Preview
            var previewService = new TargetingPreviewService(wordResolver, combatContext);
            var ammoPreviewService = new TargetingPreviewService(ammoResolver, combatContext);

            // Action execution + match service (rebuilt with composite)
            var actionExecution = new ActionExecutionService(eventBus, compositeResolver, handlerRegistry,
                combatContext, entityStats, statusEffects, animResolver);
            var wordMatchService = new WordMatchService(compositeResolver, actionRegistry);

            // Weapon executor
            var weaponExecutor = new WeaponActionExecutor(eventBus, weaponService, ammoResolver,
                handlerRegistry, combatContext, animResolver);

            // Consumable
            var consumableRegistry = ConsumableSystemInstaller.BuildConsumableRegistry(itemRegistry);
            var consumableService = new ConsumableService(eventBus, consumableRegistry);
            var consumableExecutor = new ConsumableActionExecutor(eventBus, consumableService,
                ammoResolver, handlerRegistry, combatContext, animResolver);

            // Animation service
            var animationService = new ActionAnimationService(eventBus, animResolver, handlerRegistry, entityStats);

            // Passive system
            var encounterAdapter = new ScenarioEncounterAdapter();
            encounterAdapter.SetPlayer(playerId);
            encounterAdapter.SetEventBus(eventBus);
            var triggerRegistry = PassiveSystemInstaller.CreateTriggerRegistry();
            var effectRegistry = PassiveSystemInstaller.CreateEffectRegistry(null);
            var targetResolver = new PassiveTargetResolver();
            var passiveContext = new PassiveContext(entityStats, slotService, eventBus, encounterAdapter,
                tagResolver: wordTagResolver, animationService: animationService);
            var passiveService = new PassiveService(eventBus, triggerRegistry, effectRegistry,
                targetResolver, passiveContext, allUnits);

            // Circular ref: DrunkLetterService needs StatusEffects
            drunkLetterService.SetStatusEffects(statusEffects);

            // Anxiety service (circular ref same pattern)
            var anxietyService = new AnxietyService(eventBus, wordTagResolver);
            anxietyService.SetStatusEffects(statusEffects);

            // Equipment service
            var equipmentService = new EquipmentService(eventBus, itemRegistry, entityStats,
                passiveService, weaponService, consumableService);

            // Item action handler (needs EquipmentService which depends on PassiveService)
            handlerRegistry.Register("Item", new ItemActionHandler(actionHandlerCtx,
                inventoryService, playerInventoryId, equipmentService, itemRegistry));

            // Word cooldown
            var wordCooldown = new WordCooldownService();

            // Give validator
            var giveValidator = new GiveValidator(wordTagResolver, inventoryService,
                playerInventoryId, itemRegistry, wordResolver, resourceService);

            // Spell service
            var spellService = new SpellService(eventBus, wordResolver, wordCooldown,
                spellResolver, wordTagResolver);

            // Loot reward service
            var lootRewardService = new LootRewardService(eventBus, itemRegistry,
                inventoryService, playerInventoryId, playerId, spellService, wordResolver);

            // Status visual service
            var statusVisualService = new StatusEffectVisualService(eventBus);

            // TickRunner
            var tickRunner = sceneRoot.AddComponent<TickRunner>();
            tickRunner.Initialize(new UnityTimeProvider(), new ITickable[] { animationService });

            // Run service
            var runService = new RunService(eventBus);

            return new RunSession
            {
                EventBus = eventBus,
                PlayerId = playerId,
                EntityStats = entityStats,
                WordInputService = wordInputService,
                DrunkLetterService = drunkLetterService,
                UnitService = unitService,
                WordResolver = wordResolver,
                AmmoResolver = ammoResolver,
                ActionRegistry = actionRegistry,
                WordTagResolver = wordTagResolver,
                WordMatchService = wordMatchService,
                AmmoMatchService = ammoMatchService,
                SlotService = slotService,
                CombatContext = combatContext,
                TurnService = turnService,
                StatusEffects = statusEffects,
                HandlerRegistry = handlerRegistry,
                ActionExecution = actionExecution,
                WeaponService = weaponService,
                WeaponExecutor = weaponExecutor,
                ConsumableService = consumableService,
                ConsumableExecutor = consumableExecutor,
                EquipmentService = equipmentService,
                ItemRegistry = itemRegistry,
                InventoryService = inventoryService,
                PlayerInventoryId = playerInventoryId,
                AnimationService = animationService,
                ScenarioAnimResolver = animResolver,
                PreviewService = previewService,
                AmmoPreviewService = ammoPreviewService,
                PassiveService = passiveService,
                ReactionService = reactionService,
                ReactionContext = reactionContext,
                RunService = runService,
                ResourceService = resourceService,
                LootRewardService = lootRewardService,
                StatusVisualService = statusVisualService,
                WordCooldown = wordCooldown,
                GiveValidator = giveValidator,
                SpellService = spellService,
                EnemyResolver = enemyResolver,
                SpellResolver = spellResolver,
                AllUnits = allUnits,
                AllEventEncounters = allEventEncounters,
                AnxietyService = anxietyService,
            };
        }

        private static Dictionary<string, EventEncounterDefinition> LoadEventEncounters()
        {
            var providerRegistry = EventEncounterSystemInstaller.CreateProviderRegistry();
            var result = new Dictionary<string, EventEncounterDefinition>();
            foreach (var key in providerRegistry.Keys)
            {
                if (providerRegistry.TryGet(key, out var provider))
                    result[key] = provider.CreateDefinition();
            }
            return result;
        }
    }
}
