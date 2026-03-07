using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.UnitRendering
{
    public sealed class UnitRenderingSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (IUnitService)new UnitService(eventBus);
            }, typeof(IUnitService));
        }

        public ISystemTestFactory CreateTestFactory() => new UnitRenderingTestFactory();
    }
}
