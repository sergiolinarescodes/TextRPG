using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.EntityStats
{
    public sealed class EntityStatsSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (IEntityStatsService)new EntityStatsService(eventBus);
            }, typeof(IEntityStatsService));
        }

        public ISystemTestFactory CreateTestFactory() => new EntityStatsTestFactory();
    }
}
