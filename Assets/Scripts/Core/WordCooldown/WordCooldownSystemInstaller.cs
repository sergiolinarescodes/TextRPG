using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.WordCooldown
{
    public sealed class WordCooldownSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(
                (IWordCooldownService)new WordCooldownService(),
                typeof(IWordCooldownService));
        }

        public ISystemTestFactory CreateTestFactory() => new WordCooldownTestFactory();
    }
}
