using Reflex.Core;
using TextRPG.Core.ActionExecution;
using Unidad.Core.Abstractions;
using Unidad.Core.Bootstrap;
using Unidad.Core.EventBus;
using Unidad.Core.Testing;

namespace TextRPG.Core.ActionAnimation
{
    public sealed class ActionAnimationSystemInstaller : ISystemInstaller
    {
        public void Install(ContainerBuilder builder)
        {
            builder.AddSingleton(container =>
            {
                var eventBus = container.Resolve<IEventBus>();
                var animationResolver = container.Resolve<IAnimationResolver>();
                var handlerRegistry = container.Resolve<IActionHandlerRegistry>();
                var service = new ActionAnimationService(eventBus, animationResolver, handlerRegistry);
                return (IActionAnimationService)service;
            }, typeof(IActionAnimationService));
        }

        public ISystemTestFactory CreateTestFactory() => new ActionAnimationTestFactory();
    }
}
