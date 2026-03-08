using System.Collections.Generic;
using Reflex.Core;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive.Handlers;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Passive
{
    public sealed class PassiveSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var slotService = container.Resolve<ICombatSlotService>();
                var encounterService = container.Resolve<IEncounterService>();
                var handlerRegistry = CreateHandlerRegistry();
                var context = new PassiveContext(entityStats, slotService, eventBus, encounterService);
                return (IPassiveService)new PassiveService(eventBus, handlerRegistry, context);
            }, typeof(IPassiveService));
        }

        public ISystemTestFactory CreateTestFactory() => new PassiveTestFactory();

        internal static Dictionary<string, IPassiveHandler> CreateHandlerRegistry()
        {
            return new Dictionary<string, IPassiveHandler>
            {
                [PassiveIds.HealOnAllyHit] = new HealOnAllyHitHandler(),
                [PassiveIds.HealOnRoundEnd] = new HealOnRoundEndHandler(),
                [PassiveIds.DamageOnAllyHit] = new DamageOnAllyHitHandler(),
            };
        }
    }
}
