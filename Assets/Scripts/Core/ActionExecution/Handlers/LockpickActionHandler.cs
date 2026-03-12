using TextRPG.Core.Lockpick;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class LockpickActionHandler : IActionHandler
    {
        public string ActionId => ActionNames.Lockpick;
        private readonly IEventBus _eventBus;

        public LockpickActionHandler(IActionHandlerContext ctx)
        {
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            _eventBus.Publish(new LockpickAttemptEvent(context.Source, context.Targets));
        }
    }
}
