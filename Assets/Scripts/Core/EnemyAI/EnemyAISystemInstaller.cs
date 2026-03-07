using Reflex.Core;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatGrid;
using TextRPG.Core.Encounter;
using TextRPG.Core.EnemyAI.Contributors;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Scoring;
using Unidad.Core.Testing;

namespace TextRPG.Core.EnemyAI
{
    public sealed class EnemyAISystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var encounterService = container.Resolve<IEncounterService>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var combatGrid = container.Resolve<ICombatGridService>();
                var combatContext = container.Resolve<ICombatContext>();
                var actionExecution = container.Resolve<IActionExecutionService>();
                var statusEffects = container.Resolve<IStatusEffectService>();

                var encounterImpl = (EncounterService)encounterService;
                var enemyResolver = encounterImpl.EnemyResolver;

                var scorers = CreateScorerRegistry(statusEffects);

                var turnService = container.Resolve<ITurnService>();

                return (IEnemyAIService)new EnemyAIService(eventBus, encounterService, entityStats,
                    turnService, combatGrid, combatContext, actionExecution, scorers, enemyResolver);
            }, typeof(IEnemyAIService));
        }

        public ISystemTestFactory CreateTestFactory() => new EnemyAITestFactory();

        internal static ContributorRegistry<AIDecisionContext> CreateScorerRegistry(IStatusEffectService statusEffects)
        {
            var registry = new ContributorRegistry<AIDecisionContext>();
            registry.Register(new HealthScoreContributor());
            registry.Register(new DistanceScoreContributor());
            registry.Register(new StatusEffectScoreContributor(statusEffects));
            return registry;
        }
    }
}
