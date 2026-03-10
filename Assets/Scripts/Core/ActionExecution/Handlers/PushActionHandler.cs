using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class PushActionHandler : IActionHandler
    {
        private readonly IEventBus _eventBus;

        public string ActionId => ActionNames.Push;

        public PushActionHandler(IActionHandlerContext ctx)
        {
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            for (int i = 0; i < context.Targets.Count; i++)
                _eventBus.Publish(new PushActionEvent(context.Source, context.Targets[i], context.Value));
        }
    }
}
