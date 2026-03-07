using Reflex.Core;
using TextRPG.Core.WordAction;
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

            builder.AddSingleton(container =>
            {
                var wordResolver = container.Resolve<IWordResolver>();
                var actionRegistry = container.Resolve<IActionRegistry>();
                return (IWordMatchService)new WordMatchService(wordResolver, actionRegistry);
            }, typeof(IWordMatchService));
        }

        public ISystemTestFactory CreateTestFactory() => new WordInputTestFactory();
    }
}
