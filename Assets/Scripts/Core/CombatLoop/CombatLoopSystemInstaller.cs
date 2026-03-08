using Reflex.Core;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatLoop
{
    public sealed class CombatLoopSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var turnService = container.Resolve<ITurnService>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var wordResolver = container.Resolve<IWordResolver>();
                var weaponService = container.Resolve<IWeaponService>();
                var encounterService = container.Resolve<IEncounterService>();
                var playerId = encounterService.PlayerEntity;

                return (ICombatLoopService)new CombatLoopService(
                    eventBus, turnService, entityStats, wordResolver, weaponService, playerId);
            }, typeof(ICombatLoopService));
        }

        public ISystemTestFactory CreateTestFactory() => new CombatLoopTestFactory();
    }
}
