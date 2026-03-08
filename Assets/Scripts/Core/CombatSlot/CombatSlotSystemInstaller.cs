using Reflex.Core;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatSlot
{
    public sealed class CombatSlotSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                return (ICombatSlotService)new CombatSlotService(eventBus);
            }, typeof(ICombatSlotService));
        }

        public ISystemTestFactory CreateTestFactory() => new CombatSlotTestFactory();
    }
}
