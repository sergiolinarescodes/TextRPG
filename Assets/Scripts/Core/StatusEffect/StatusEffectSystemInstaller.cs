using Reflex.Core;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect.Handlers;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.StatusEffect
{
    public sealed class StatusEffectSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var turnService = container.Resolve<ITurnService>();
                var handlerRegistry = CreateHandlerRegistry();
                var handlerContext = new StatusEffectHandlerContext(entityStats, turnService, eventBus);

                IEncounterService encounterService = null;
                try { encounterService = container.Resolve<IEncounterService>(); } catch { /* optional */ }
                handlerContext.EncounterService = encounterService;

                var service = new StatusEffectService(eventBus, entityStats, turnService, handlerRegistry, handlerContext);
                handlerContext.StatusEffects = service;
                return (IStatusEffectService)service;
            }, typeof(IStatusEffectService));
        }

        public ISystemTestFactory CreateTestFactory() => new StatusEffectTestFactory();

        internal static StatusEffectHandlerRegistry CreateHandlerRegistry()
        {
            var registry = new StatusEffectHandlerRegistry();
            registry.Register(StatusEffectType.Burning, new BurningHandler());
            registry.Register(StatusEffectType.Poisoned, new PoisonedHandler());
            registry.Register(StatusEffectType.Wet, new WetHandler());
            registry.Register(StatusEffectType.Slowed, new SlowedHandler());
            registry.Register(StatusEffectType.Cursed, new CursedHandler());
            registry.Register(StatusEffectType.Buffed, new BuffedHandler());
            registry.Register(StatusEffectType.Shielded, new ShieldedHandler());
            registry.Register(StatusEffectType.ExtraTurn, new ExtraTurnHandler());
            registry.Register(StatusEffectType.Frozen, new FrozenHandler());
            registry.Register(StatusEffectType.Stun, new StunHandler());
            registry.Register(StatusEffectType.Concussion, new ConcussionHandler());
            registry.Register(StatusEffectType.Fear, new FearHandler());
            registry.Register(StatusEffectType.Bleeding, new BleedingHandler());
            registry.Register(StatusEffectType.Concentrated, new ConcentratedHandler());
            registry.Register(StatusEffectType.Growing, new GrowingHandler());
            registry.Register(StatusEffectType.Thorns, new ThornsHandler());
            registry.Register(StatusEffectType.Reflecting, new ReflectingHandler());
            registry.Register(StatusEffectType.Hardening, new HardeningHandler());
            registry.Register(StatusEffectType.Drunk, new DrunkHandler());
            return registry;
        }
    }
}
