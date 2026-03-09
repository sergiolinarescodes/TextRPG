using Reflex.Core;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Equipment
{
    public sealed class EquipmentSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var data = container.Resolve<WordActionData>();
                return BuildItemRegistry(data);
            }, typeof(IItemRegistry));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var itemRegistry = container.Resolve<IItemRegistry>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var passiveService = container.Resolve<IPassiveService>();
                var weaponService = container.Resolve<IWeaponService>();
                return (IEquipmentService)new EquipmentService(
                    eventBus, itemRegistry, entityStats, passiveService, weaponService);
            }, typeof(IEquipmentService));
        }

        internal static IItemRegistry BuildItemRegistry(WordActionData data)
        {
            var items = ItemDatabaseLoader.LoadAll(data);
            var registry = new ItemRegistry();
            foreach (var (key, def) in items)
                registry.Register(key, def);
            return registry;
        }

        public ISystemTestFactory CreateTestFactory() => new EquipmentTestFactory();
    }
}
