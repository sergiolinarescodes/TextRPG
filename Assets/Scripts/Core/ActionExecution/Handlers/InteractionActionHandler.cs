using TextRPG.Core.EventEncounter;
using Unidad.Core.EventBus;

namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class InteractionActionHandler : IActionHandler
    {
        private readonly IEventBus _eventBus;

        public string ActionId { get; }

        public InteractionActionHandler(string actionId, IActionHandlerContext ctx)
        {
            ActionId = actionId;
            _eventBus = ctx.EventBus;
        }

        public void Execute(ActionContext context)
        {
            _eventBus.Publish(new InteractionActionEvent(
                context.Source, ActionId, context.Targets, context.Value, context.Word));
        }
    }
}
