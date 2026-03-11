using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Effects;
using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter.Encounters;
using TextRPG.Core.EventEncounter.Reactions;
using TextRPG.Core.EventEncounter.Reactions.Tags;
using TextRPG.Core.Passive;
using TextRPG.Core.Services;
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

                var outcomeRegistry = CreateOutcomeRegistry(null);
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

        internal static InteractionOutcomeRegistry CreateOutcomeRegistry(IGameServices services)
        {
            var registry = AssemblyScanner.BuildRegistry<InteractionOutcomeRegistry, IInteractionOutcome>();

            if (services != null)
            {
                // Override shared effects with adapters that use IGameServices
                var effectRegistry = PassiveSystemInstaller.CreateGameEffectRegistry();
                foreach (var effect in effectRegistry.Values)
                {
                    var swap = effect.EffectId == "damage";
                    registry.Remove(effect.EffectId);
                    registry.Register(effect.EffectId, new OutcomeEffectAdapter(effect, services, swap));
                }
            }

            return registry;
        }

        internal static TagReactionRegistry CreateTagReactionRegistry()
            => AssemblyScanner.BuildRegistry<TagReactionRegistry, ITagDefinition>();
    }
}
