using Reflex.Core;
using TextRPG.Core.ActionExecution;
using TextRPG.Core.CombatSlot;
using TextRPG.Core.CombatAI.Contributors;
using TextRPG.Core.Encounter;
using TextRPG.Core.EntityStats;
using TextRPG.Core.Passive;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Patterns.Scoring;
using Unidad.Core.Testing;

namespace TextRPG.Core.CombatAI
{
    public sealed class CombatAISystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var encounterService = container.Resolve<IEncounterService>();
                var entityStats = container.Resolve<IEntityStatsService>();
                var slotService = container.Resolve<ICombatSlotService>();
                var combatContext = container.Resolve<ICombatContext>();
                var actionExecution = container.Resolve<IActionExecutionService>();
                var statusEffects = container.Resolve<IStatusEffectService>();

                var encounterImpl = (EncounterService)encounterService;
                var enemyResolver = encounterImpl.EnemyResolver;

                var scorers = CreateScorerRegistry(statusEffects);

                var turnService = container.Resolve<ITurnService>();
                var passiveService = container.Resolve<IPassiveService>();

                return (ICombatAIService)new CombatAIService(eventBus, encounterService, entityStats,
                    turnService, slotService, combatContext, actionExecution, scorers, enemyResolver,
                    passiveService: passiveService);
            }, typeof(ICombatAIService));
        }

        public ISystemTestFactory CreateTestFactory() => new CombatAITestFactory();

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
