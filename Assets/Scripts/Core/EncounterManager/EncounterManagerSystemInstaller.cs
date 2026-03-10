using TextRPG.Core.Encounter;
using TextRPG.Core.EventEncounter;
using Reflex.Core;
using Unidad.Core.EventBus;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.EncounterManager
{
    public sealed class EncounterManagerSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var combatEncounter = container.Resolve<IEncounterService>();
                var eventEncounter = container.Resolve<IEventEncounterService>();
                var providerRegistry = container.Resolve<EventEncounterProviderRegistry>();

                return (IEncounterManager)new EncounterManager(eventBus, combatEncounter, eventEncounter, providerRegistry);
            }, typeof(IEncounterManager));

            builder.AddSingleton(container =>
                (ICombatModeService)container.Resolve<IEncounterManager>(), typeof(ICombatModeService));
        }

        public ISystemTestFactory CreateTestFactory() => new EncounterManagerTestFactory();
    }
}
