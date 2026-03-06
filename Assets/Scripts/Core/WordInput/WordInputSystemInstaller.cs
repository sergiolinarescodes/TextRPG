using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordInput
{
    public sealed class WordInputSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (IWordInputService)new WordInputService(eventBus);
            }, typeof(IWordInputService));
        }

        public ISystemTestFactory CreateTestFactory() => new WordInputTestFactory();
    }
}
