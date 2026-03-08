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

            // Scan all words for Weapon actions and build WeaponDefinitions
            var weaponAmmo = new Dictionary<string, (int durability, List<string> ammo)>(StringComparer.OrdinalIgnoreCase);

            // We need to iterate all words — get them from the resolver
            // WordResolver exposes all words via its mappings
            // We check each word's actions for "Weapon" entries
            foreach (var word in resolver.AllWords)
            {
                var actions = resolver.Resolve(word);
                for (int i = 0; i < actions.Count; i++)
                {
                    var mapping = actions[i];
                    if (!string.Equals(mapping.ActionId, "Weapon", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!weaponAmmo.TryGetValue(word, out var entry))
                    {
                        entry = (mapping.Value, new List<string>());
                        weaponAmmo[word] = entry;
                    }

                    if (!string.IsNullOrEmpty(mapping.AssocWord))
                        entry.ammo.Add(mapping.AssocWord.ToLowerInvariant());
                }
            }

            foreach (var (weaponWord, (durability, ammo)) in weaponAmmo)
            {
                var def = new WeaponDefinition(
                    weaponWord,
                    weaponWord.ToUpperInvariant(),
                    durability,
                    ammo);
                registry.Register(weaponWord, def);
            }

            return registry;
        }

        public ISystemTestFactory CreateTestFactory() => new WeaponTestFactory();
    }
}
