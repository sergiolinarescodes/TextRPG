using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Experience
{
    public sealed class ExperienceSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (IExperienceService)new ExperienceService(eventBus);
            }, typeof(IExperienceService));
        }

        public ISystemTestFactory CreateTestFactory() => new ExperienceTestFactory();
    }
}
