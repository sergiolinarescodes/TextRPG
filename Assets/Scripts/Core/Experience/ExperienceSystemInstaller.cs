using Reflex.Core;
using TextRPG.Core.Equipment;
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
                var lootRewardService = container.Resolve<ILootRewardService>();
                return (IExperienceService)new ExperienceService(eventBus, lootRewardService);
            }, typeof(IExperienceService));
        }

        public ISystemTestFactory CreateTestFactory() => new ExperienceTestFactory();
    }
}
