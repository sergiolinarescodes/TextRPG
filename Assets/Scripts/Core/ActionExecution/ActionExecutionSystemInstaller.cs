using Reflex.Core;
using TextRPG.Core.EntityStats;
using TextRPG.Core.StatusEffect;
using TextRPG.Core.TurnSystem;
using TextRPG.Core.Weapon;
using TextRPG.Core.WordAction;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.ActionExecution
{
    public sealed class ActionExecutionSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var ctx = new CombatContext();
                ctx.SetEntityStats(container.Resolve<IEntityStatsService>());
                ctx.SetStatusEffects(container.Resolve<IStatusEffectService>());
                return (ICombatContext)ctx;
            }, typeof(ICombatContext));

            builder.AddSingleton(container =>
            {
                var handlerContext = new ActionHandlerContext(
                    container.Resolve<IEntityStatsService>(),
                    container.Resolve<IEventBus>(),
                    container.Resolve<ICombatContext>(),
                    container.Resolve<IStatusEffectService>(),
                    container.Resolve<ITurnService>(),
                    container.Resolve<IWeaponService>());

                var registry = ActionHandlerRegistryFactory.CreateDefault(handlerContext);
                return (IActionHandlerRegistry)registry;
            }, typeof(IActionHandlerRegistry));

            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var wordResolver = container.Resolve<IWordResolver>();
                var handlerRegistry = container.Resolve<IActionHandlerRegistry>();
                var combatContext = container.Resolve<ICombatContext>();
                return (IActionExecutionService)new ActionExecutionService(eventBus, wordResolver, handlerRegistry, combatContext);
            }, typeof(IActionExecutionService));

            builder.AddSingleton(container =>
            {
                var wordResolver = container.Resolve<IWordResolver>();
                var combatContext = container.Resolve<ICombatContext>();
                return (ITargetingPreviewService)new TargetingPreviewService(wordResolver, combatContext);
            }, typeof(ITargetingPreviewService));
        }

        public ISystemTestFactory CreateTestFactory() => new ActionExecutionTestFactory();
    }
}
