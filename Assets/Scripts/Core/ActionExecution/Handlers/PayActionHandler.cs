namespace TextRPG.Core.ActionExecution.Handlers
{
    internal sealed class PayActionHandler : IActionHandler
    {
        public string ActionId => ActionNames.Pay;

        public void Execute(ActionContext context)
        {
            // No-op: the real work happens via ReactionService reacting to ActionHandlerExecutedEvent
        }
    }
}
