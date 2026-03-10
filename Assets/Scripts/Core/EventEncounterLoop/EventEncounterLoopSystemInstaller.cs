using TextRPG.Core.EntityStats;
using TextRPG.Core.EventEncounter;
using TextRPG.Core.WordAction;
using Reflex.Core;
using Unidad.Core.EventBus;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.EventEncounterLoop
{
    public sealed class EventEncounterLoopSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var wordResolver = container.Resolve<IWordResolver>();
                var encounterService = container.Resolve<IEventEncounterService>();
                var playerId = encounterService.PlayerEntity;

                return (IEventEncounterLoopService)new EventEncounterLoopService(
                    eventBus, entityStats, wordResolver, encounterService, playerId);
            }, typeof(IEventEncounterLoopService));
        }

        public ISystemTestFactory CreateTestFactory() => new EventEncounterLoopTestFactory();
    }
}
