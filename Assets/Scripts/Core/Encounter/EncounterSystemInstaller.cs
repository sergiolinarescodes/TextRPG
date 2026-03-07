using Reflex.Core;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.EntityStats;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.Encounter
{
    public sealed class EncounterSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var turnService = container.Resolve<ITurnService>();
                var combatGrid = container.Resolve<ICombatGridService>();
                var combatContext = container.Resolve<ICombatContext>();
                var enemyResolver = new EnemyWordResolver();
                return (IEncounterService)new EncounterService(eventBus, entityStats, turnService, combatGrid, combatContext, enemyResolver);
            }, typeof(IEncounterService));
        }

        public ISystemTestFactory CreateTestFactory() => new EncounterTestFactory();
    }
}
