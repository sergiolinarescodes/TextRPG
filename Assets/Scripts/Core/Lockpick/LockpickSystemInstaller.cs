using Reflex.Core;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Luck;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Lockpick
{
    public sealed class LockpickSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var luckService = container.Resolve<ILuckService>();
                return (ILockpickService)new LockpickService(eventBus, entityStats, luckService);
            }, typeof(ILockpickService));
        }

        public ISystemTestFactory CreateTestFactory() => new LockpickTestFactory();
    }
}
