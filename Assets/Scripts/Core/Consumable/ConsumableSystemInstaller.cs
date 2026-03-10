using System;
using Reflex.Core;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.Equipment;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Consumable
{
    public sealed class ConsumableSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var itemRegistry = container.Resolve<IItemRegistry>();
                return BuildConsumableRegistry(itemRegistry);
            }, typeof(IConsumableRegistry));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var registry = container.Resolve<IConsumableRegistry>();
                return (IConsumableService)new ConsumableService(eventBus, registry);
            }, typeof(IConsumableService));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var consumableService = container.Resolve<IConsumableService>();
                var data = container.Resolve<WordActionData>();
                var handlerRegistry = container.Resolve<IActionHandlerRegistry>();
                var combatContext = container.Resolve<ICombatContext>();
                var animationResolver = container.Resolve<IAnimationResolver>();
                return new ConsumableActionExecutor(
                    eventBus, consumableService, data.AmmoResolver, handlerRegistry, combatContext, animationResolver);
            });
        }

        internal static IConsumableRegistry BuildConsumableRegistry(IItemRegistry itemRegistry)
        {
            var registry = new ConsumableRegistry();
            foreach (var key in itemRegistry.Keys)
            {
                if (!itemRegistry.TryGet(key, out var itemDef)) continue;
                if (itemDef.SlotType != EquipmentSlotType.Consumable) continue;

                var def = new ConsumableDefinition(
                    itemDef.ItemWord,
                    itemDef.DisplayName,
                    itemDef.Durability,
                    itemDef.AmmoWords ?? Array.Empty<string>());
                registry.Register(itemDef.ItemWord, def);
            }
            return registry;
        }

        public ISystemTestFactory CreateTestFactory() => new ConsumableTestFactory();
    }
}
