using Reflex.Core;
using TextRPG.Core.EntityStats;
using Unidad.Core.Bootstrap;
using Unidad.Core.Testing;

namespace TextRPG.Core.Luck
{
    public sealed class LuckSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var entityStats = container.Resolve<IEntityStatsService>();
                return (ILuckService)new LuckService(entityStats);
            }, typeof(ILuckService));
        }

        public ISystemTestFactory CreateTestFactory() => new LuckTestFactory();
    }
}
