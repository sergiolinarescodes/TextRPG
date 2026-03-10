using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Run
{
    public sealed class RunSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (IRunService)new RunService(eventBus);
            }, typeof(IRunService));
        }

        public ISystemTestFactory CreateTestFactory() => new RunTestFactory();
    }
}
