using Reflex.Core;
using TextRPG.Core.Encounter;
using TextRPG.Core.WordAction;
using TextRPG.Core.WordCooldown;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Scroll
{
    public sealed class ScrollSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(_ => new EnemyWordResolver(), typeof(EnemyWordResolver));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var baseResolver = container.Resolve<IWordResolver>();
                var cooldown = container.Resolve<IWordCooldownService>();
                var spellResolver = container.Resolve<EnemyWordResolver>();
                return (ISpellService)new SpellService(eventBus, baseResolver, cooldown, spellResolver);
            }, typeof(ISpellService));
        }

        public ISystemTestFactory CreateTestFactory() => new ScrollTestFactory();
    }
}
