using Reflex.Core;
using TextRPG.Core.UnitRendering;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatGrid
{
    public sealed class CombatGridSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var unitService = container.Resolve<IUnitService>();
                return (ICombatGridService)new CombatGridService(eventBus, unitService);
            }, typeof(ICombatGridService));
        }

        public ISystemTestFactory CreateTestFactory() => new CombatGridTestFactory();
    }
}
