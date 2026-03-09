using System;
using System.Collections.Generic;
using Reflex.Core;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.WordAction;
using Unidad.Core.Abstractions;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Weapon
{
    public sealed class WeaponSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var data = container.Resolve<WordActionData>();
                return BuildWeaponRegistry(data);
            }, typeof(IWeaponRegistry));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var registry = container.Resolve<IWeaponRegistry>();
                return (IWeaponService)new WeaponService(eventBus, registry);
            }, typeof(IWeaponService));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var weaponService = container.Resolve<IWeaponService>();
                var data = container.Resolve<WordActionData>();
                var handlerRegistry = container.Resolve<IActionHandlerRegistry>();
                var combatContext = container.Resolve<ICombatContext>();
                var animationResolver = container.Resolve<IAnimationResolver>();
                return (IWeaponActionExecutor)new WeaponActionExecutor(
                    eventBus, weaponService, data.AmmoResolver, handlerRegistry, combatContext, animationResolver);
            }, typeof(IWeaponActionExecutor));
        }

        internal static IWeaponRegistry BuildWeaponRegistry(WordActionData data)
        {
            var registry = new WeaponRegistry();
            var resolver = data.Resolver;
            var ammoByItem = data.AmmoWordsByItem;

            // Build WeaponDefinitions from items that have Item/Weapon actions
            foreach (var (itemWord, ammoList) in ammoByItem)
            {
                // Get durability from the first Item/Weapon action mapping
                var actions = resolver.Resolve(itemWord);
                int durability = 0;
                for (int i = 0; i < actions.Count; i++)
                {
                    var mapping = actions[i];
                    if (string.Equals(mapping.ActionId, "Weapon", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(mapping.ActionId, "Item", StringComparison.OrdinalIgnoreCase))
                    {
                        durability = mapping.Value;
                        break;
                    }
                }

                var def = new WeaponDefinition(
                    itemWord,
                    itemWord.ToUpperInvariant(),
                    durability,
                    ammoList);
                registry.Register(itemWord, def);
            }

            return registry;
        }

        public ISystemTestFactory CreateTestFactory() => new WeaponTestFactory();
    }
}
