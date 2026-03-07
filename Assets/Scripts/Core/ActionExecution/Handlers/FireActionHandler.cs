using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class FireActionHandler : IActionHandler
    {
        private readonly IEventBus _eventBus;

        public string ActionId => "Fire";

        public FireActionHandler(IActionHandlerContext ctx)
        {
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            _eventBus.Publish(new FireGridStatusEvent(context.Source, context.Value));
        }
    }
}
