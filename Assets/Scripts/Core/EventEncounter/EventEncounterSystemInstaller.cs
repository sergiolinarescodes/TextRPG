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
                var reactionService = new ReactionService(eventBus, outcomeRegistry, encounterContext, tagReactions);
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
            return registry;
        }

        internal static TagReactionRegistry CreateTagReactionRegistry()
        {
            var registry = new TagReactionRegistry();

            registry.Register("flammable", "Burn", new InteractionReaction("Burn", "damage_target", null, 3));
            registry.Register("flammable", "Burn", new InteractionReaction("Burn", "message", "It catches fire!", 0));
            registry.Register("flammable", "Fire", new InteractionReaction("Fire", "message", "It catches fire!", 0));

            registry.Register("meltable", "Melt", new InteractionReaction("Melt", "consume", null, 0));
            registry.Register("meltable", "Melt", new InteractionReaction("Melt", "message", "It melts open!", 0));

            registry.Register("breakable", "Damage", new InteractionReaction("Damage", "message", "It cracks under the force!", 0));

            registry.Register("conductive", "Shock", new InteractionReaction("Shock", "damage_target", null, 2));
            registry.Register("conductive", "Shock", new InteractionReaction("Shock", "message", "Electricity surges through it!", 0));

            return registry;
        }
    }
}
