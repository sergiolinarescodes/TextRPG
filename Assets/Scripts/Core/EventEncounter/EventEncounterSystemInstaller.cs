using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Encounters;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounter.Reactions.Outcomes;
using TextRPG.Core.StatusEffect;
using Reflex.Core;
using Unidad.Core.EventBus;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.EventEncounter
{
    public sealed class EventEncounterSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            var providerRegistry = CreateProviderRegistry();
            builder.AddSingleton(providerRegistry, typeof(EventEncounterProviderRegistry));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var slotService = container.Resolve<ICombatSlotService>();
                var combatContext = container.Resolve<ICombatContext>();

                IStatusEffectService statusEffects = null;
                try { statusEffects = container.Resolve<IStatusEffectService>(); } catch { /* optional */ }

                var outcomeRegistry = CreateOutcomeRegistry();
                var tagReactions = CreateTagReactionRegistry();
                var encounterContext = new EventEncounterContext(entityStats, slotService, eventBus, statusEffects);
                var reactionService = new ReactionService(eventBus, outcomeRegistry, encounterContext, tagReactions, combatContext);
                var service = new EventEncounterService(eventBus, entityStats, slotService, combatContext, reactionService);
                encounterContext.EncounterService = service;

                return (IEventEncounterService)service;
            }, typeof(IEventEncounterService));
        }

        public ISystemTestFactory CreateTestFactory() => new EventEncounterTestFactory();

        internal static EventEncounterProviderRegistry CreateProviderRegistry()
        {
            var registry = new EventEncounterProviderRegistry();
            registry.Register("test_inn", new RoadsideInnEncounter());
            registry.Register("test_chest", new HiddenChestEncounter());
            registry.Register("test_shrine", new AncientShrineEncounter());
            registry.Register("test_iron_vault", new IronVaultEncounter());
            registry.Register("test_armored_crate", new ArmoredCrateEncounter());
            registry.Register("tavern", new TavernEncounter());
            registry.Register("fruit_shop", new FruitShopEncounter());
            EventEncounterDatabaseLoader.LoadIntoRegistry(registry);
            return registry;
        }

        internal static InteractionOutcomeRegistry CreateOutcomeRegistry()
        {
            var registry = new InteractionOutcomeRegistry();
            registry.Register("message", new MessageOutcome());
            registry.Register("heal", new HealOutcome());
            registry.Register("damage", new DamageOutcome());
            registry.Register("damage_target", new DamageInteractableOutcome());
            registry.Register("shield", new ShieldOutcome());
            registry.Register("mana", new ManaOutcome());
            registry.Register("transition", new TransitionOutcome());
            registry.Register("consume", new ConsumeOutcome());
            registry.Register("reward", new RewardOutcome());
            registry.Register("apply_status", new ApplyStatusOutcome());
            registry.Register("spawn_combat", new SpawnCombatOutcome());
            registry.Register("recruit", new RecruitOutcome());
            registry.Register("leave", new LeaveOutcome());
            registry.Register("give_item", new GiveItemOutcome());
            return registry;
        }

        internal static TagReactionRegistry CreateTagReactionRegistry()
        {
            var registry = new TagReactionRegistry();
            registry.Register(new Reactions.Tags.Definitions.FlammableTagDefinition());
            registry.Register(new Reactions.Tags.Definitions.MeltableTagDefinition());
            registry.Register(new Reactions.Tags.Definitions.BreakableTagDefinition());
            registry.Register(new Reactions.Tags.Definitions.ConductiveTagDefinition());
            registry.Register(new Reactions.Tags.Definitions.SocialTagDefinition());
            registry.Register(new Reactions.Tags.Definitions.MercenaryTagDefinition());
            return registry;
        }
    }
}
