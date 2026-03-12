using Reflex.Core;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using TextRPG.Core.Services;
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

                ILuckService luckService = null;
                try { luckService = container.Resolve<ILuckService>(); } catch { /* optional */ }
                handlerContext.LuckService = luckService;

                var service = new StatusEffectService(eventBus, entityStats, turnService, handlerRegistry, handlerContext, luckService: luckService);
                handlerContext.StatusEffects = service;
                return (IStatusEffectService)service;
            }, typeof(IStatusEffectService));
        }

        public ISystemTestFactory CreateTestFactory() => new StatusEffectTestFactory();

        internal static StatusEffectHandlerRegistry CreateHandlerRegistry()
            => AssemblyScanner.BuildRegistry<StatusEffectHandlerRegistry, IStatusEffectHandler>();
    }
}
