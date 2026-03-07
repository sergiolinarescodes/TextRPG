namespace TextRPG.Core.ActionExecution
{
    public interface IActionHandler
    {
        string ActionId { get; }
        void Execute(ActionContext context);
    }
}
