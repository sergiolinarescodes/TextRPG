using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordAction
{
    public sealed class WordActionSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var data = WordActionDatabaseLoader.Load();
                return data;
            });

            builder.AddSingleton(container =>
            {
                var data = container.Resolve<WordActionData>();
                return data.Resolver;
            }, typeof(IWordResolver));

            builder.AddSingleton(container =>
            {
                var data = container.Resolve<WordActionData>();
                return data.ActionRegistry;
            }, typeof(IActionRegistry));
        }

        public ISystemTestFactory CreateTestFactory() => new WordActionTestFactory();
    }
}
