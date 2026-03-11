namespace TextRPG.Core.ActionExecution
{
    public sealed record ActionTemplateDef(
        string ActionId,
        string Template,
        string Param1 = null,
        string Param2 = null,
        bool ApplySelf = false
    );
}
