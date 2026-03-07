using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.TurnSystem
{
    public sealed class TurnSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (ITurnService)new TurnService(eventBus);
            }, typeof(ITurnService));
        }

        public ISystemTestFactory CreateTestFactory() => new TurnTestFactory();
    }
}
